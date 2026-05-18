# Sentry bug reporting

RoobsWorld uses the official Sentry Unity SDK package from `https://github.com/getsentry/unity.git#4.3.1`.

Bug reporting is off unless a DSN is supplied. Configure it at launch with either:

- Environment variable: `ROOBSWORLD_SENTRY_DSN=https://publicKey@host/projectId`
- Fallback environment variable: `SENTRY_DSN=https://publicKey@host/projectId`
- Launch argument: `--sentry-dsn=https://publicKey@host/projectId`

Optional launch/env overrides:

- `ROOBSWORLD_SENTRY_ENVIRONMENT` or `--sentry-environment=playtest`
- `ROOBSWORLD_SENTRY_RELEASE` or `--sentry-release=RoobsWorld@1.0.0`
- `ROOBSWORLD_SENTRY_DEBUG=true` or `--sentry-debug=true`
- `ROOBSWORLD_SENTRY_ENABLED=false` or `--sentry-enabled=false`
- `ROOBSWORLD_SENTRY_CAPTURE_IN_EDITOR=true` or `--sentry-capture-in-editor=true`

Editor reporting is disabled by default to avoid sending local test/compiler failures. Player builds report as `production` by default, or `development` for debug builds.

The runtime setup keeps default PII off, captures Unity `Debug.LogError` events and unhandled exceptions, and adds game/platform/build tags before sending events.

When Sentry is configured, the pause menu shows a `Report Bug` action. It sends the player description, optional email, and a screenshot through Sentry user feedback. When no DSN is configured, that action stays hidden.
