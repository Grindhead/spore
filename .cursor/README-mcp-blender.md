# Blender MCP connection

Cursor talks to Blender via the **Blender MCP Server** (`blender-mcp-server` on PyPI). The server is configured in `.cursor/mcp.json`.

## Requirements

- **Python 3.10+** (for `uvx` / MCP server)
- **Blender 3.0+**
- **Blender addon** (TCP bridge) installed and enabled in Blender

## 1. Blender addon (TCP bridge)

The MCP server talks to Blender over TCP. Blender must run the addon that listens on `localhost:9876`.

1. Get the addon from the repo: [djeada/blender-mcp-server](https://github.com/djeada/blender-mcp-server) — use the `addon` folder (or `addon/__init__.py` as the installable).
2. In Blender: **Edit → Preferences → Add-ons → Install…** and select the addon file.
3. Enable **Blender MCP Bridge**.
4. Confirm the addon shows **● Listening on 127.0.0.1:9876** in the sidebar.

## 2. Cursor MCP config

`.cursor/mcp.json` is already set to run the server with:

- **Command:** `C:\Users\craig\.local\bin\uvx.exe`
- **Args:** `["blender-mcp-server"]`

So no `pip install` is required; `uvx` runs `blender-mcp-server` on demand.

## 3. Using it

1. Start **Blender** with the addon enabled (listening on 9876).
2. In Cursor, the **blender** MCP server will connect to that Blender instance.
3. You can ask Cursor to create objects, materials, render, export, etc., and it will call the MCP tools that control Blender.

If the Blender addon is not running or the port is wrong, MCP tool calls to Blender will fail until Blender is listening on `localhost:9876`.
