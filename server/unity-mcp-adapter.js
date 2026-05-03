#!/usr/bin/env node
"use strict";

const http = require("http");
const { URL } = require("url");

const PORT = Number(process.env.PORT || 7788);
const UNITY_API_URL = process.env.UNITY_API_URL || "http://localhost:7797/";
const MAX_BODY_BYTES = 1024 * 1024; // 1 MiB

const KNOWN_UNITY_ACTIONS = [
  "create_object",
  "edit_object",
  "generate_prefab",
  "run_build",
  "trigger_blender_export",
  "scatter_objects",
  "generate_level",
  "generate_full_level",
  "populate_level",
  "wire_events",
  "generate_playable_dungeon",
  "run_quick_setup",
  "run_tests",
  "set_component_field"
];

function jsonRpcResult(id, result) {
  return { jsonrpc: "2.0", id, result };
}

function jsonRpcError(id, code, message, data) {
  const error = { code, message };
  if (data !== undefined) error.data = data;
  return { jsonrpc: "2.0", id, error };
}

function makeTool(name, description) {
  return {
    name,
    description,
    inputSchema: {
      type: "object",
      additionalProperties: true
    }
  };
}

const TOOLS = [
  makeTool(
    "unity_action",
    "Call any Unity action by passing { action, parameters } to the Unity HTTP server."
  ),
  ...KNOWN_UNITY_ACTIONS.map((action) =>
    makeTool(action, `Call Unity action '${action}' with tool arguments as action parameters.`)
  )
];

async function postUnityAction(action, parameters) {
  const payload = JSON.stringify({
    action,
    parameters: parameters && typeof parameters === "object" ? parameters : {}
  });

  const response = await fetch(UNITY_API_URL, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: payload
  });

  const text = await response.text();
  if (!response.ok) {
    throw new Error(`Unity server HTTP ${response.status}: ${text}`);
  }
  return text;
}

function toolErrorResult(message) {
  return {
    isError: true,
    content: [{ type: "text", text: message }]
  };
}

function toolTextResult(text) {
  return {
    content: [{ type: "text", text }]
  };
}

async function handleRpcMessage(message) {
  if (!message || typeof message !== "object") {
    return jsonRpcError(null, -32600, "Invalid Request");
  }

  const hasId = Object.prototype.hasOwnProperty.call(message, "id");
  const id = hasId ? message.id : null;
  const method = message.method;
  const params = message.params || {};

  if (typeof method !== "string") {
    return hasId ? jsonRpcError(id, -32600, "Invalid Request") : null;
  }

  try {
    switch (method) {
      case "initialize": {
        const requestedVersion = params.protocolVersion;
        return jsonRpcResult(id, {
          protocolVersion: requestedVersion || "2025-03-26",
          capabilities: { tools: {} },
          serverInfo: {
            name: "unity-mcp-adapter",
            version: "1.0.0"
          }
        });
      }

      case "notifications/initialized":
        return hasId ? jsonRpcResult(id, {}) : null;

      case "ping":
        return jsonRpcResult(id, {});

      case "tools/list":
        return jsonRpcResult(id, { tools: TOOLS });

      case "tools/call": {
        const toolName = params.name;
        const args = params.arguments || {};

        if (typeof toolName !== "string" || !toolName) {
          return jsonRpcResult(id, toolErrorResult("Missing tool name."));
        }

        if (toolName !== "unity_action" && !KNOWN_UNITY_ACTIONS.includes(toolName)) {
          return jsonRpcResult(
            id,
            toolErrorResult(`Unknown tool '${toolName}'. Use tools/list to inspect available tools.`)
          );
        }

        let action;
        let actionParameters;

        if (toolName === "unity_action") {
          action = args.action;
          actionParameters = args.parameters || {};
          if (typeof action !== "string" || !action.trim()) {
            return jsonRpcResult(
              id,
              toolErrorResult("unity_action requires 'action' (string) and optional 'parameters' object.")
            );
          }
          action = action.trim();
        } else {
          action = toolName;
          actionParameters = args;
        }

        try {
          const unityResponse = await postUnityAction(action, actionParameters);
          return jsonRpcResult(id, toolTextResult(unityResponse));
        } catch (error) {
          const messageText = error instanceof Error ? error.message : String(error);
          return jsonRpcResult(id, toolErrorResult(`Unity call failed: ${messageText}`));
        }
      }

      default:
        return hasId ? jsonRpcError(id, -32601, `Method not found: ${method}`) : null;
    }
  } catch (error) {
    const messageText = error instanceof Error ? error.message : String(error);
    return hasId ? jsonRpcError(id, -32603, "Internal error", messageText) : null;
  }
}

function sendJson(res, statusCode, payload) {
  const data = JSON.stringify(payload);
  res.writeHead(statusCode, {
    "content-type": "application/json",
    "content-length": Buffer.byteLength(data)
  });
  res.end(data);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let total = 0;

    req.on("data", (chunk) => {
      total += chunk.length;
      if (total > MAX_BODY_BYTES) {
        reject(new Error("Request body too large."));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });

    req.on("end", () => {
      resolve(Buffer.concat(chunks).toString("utf8"));
    });

    req.on("error", reject);
  });
}

const server = http.createServer(async (req, res) => {
  const requestUrl = new URL(req.url || "/", `http://${req.headers.host || "localhost"}`);

  if (req.method === "GET" && requestUrl.pathname === "/health") {
    return sendJson(res, 200, {
      ok: true,
      service: "unity-mcp-adapter",
      unityApiUrl: UNITY_API_URL
    });
  }

  if (req.method !== "POST" || requestUrl.pathname !== "/") {
    return sendJson(res, 404, { error: "Not found" });
  }

  try {
    const rawBody = await readBody(req);
    if (!rawBody.trim()) {
      return sendJson(res, 400, jsonRpcError(null, -32700, "Parse error: empty body"));
    }

    let parsed;
    try {
      parsed = JSON.parse(rawBody);
    } catch {
      return sendJson(res, 400, jsonRpcError(null, -32700, "Parse error: invalid JSON"));
    }

    if (Array.isArray(parsed)) {
      if (parsed.length === 0) {
        return sendJson(res, 400, jsonRpcError(null, -32600, "Invalid Request"));
      }

      const responses = [];
      for (const msg of parsed) {
        const response = await handleRpcMessage(msg);
        if (response) responses.push(response);
      }

      if (responses.length === 0) {
        res.writeHead(204);
        return res.end();
      }
      return sendJson(res, 200, responses);
    }

    const response = await handleRpcMessage(parsed);
    if (!response) {
      res.writeHead(204);
      return res.end();
    }
    return sendJson(res, 200, response);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    return sendJson(res, 500, jsonRpcError(null, -32603, "Internal server error", message));
  }
});

server.listen(PORT, "127.0.0.1", () => {
  console.log(
    `[unity-mcp-adapter] listening on http://127.0.0.1:${PORT}/ (forwarding to ${UNITY_API_URL})`
  );
});
