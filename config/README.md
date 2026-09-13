# Runtime configuration

Non-secret defaults live in `appsettings.json` (plus `appsettings.Development.json` /
`appsettings.Windows.json` overlays) and are embedded into Core. Remaining theme
directories keep files that are not folded into those overlays. Environment or
deployment providers may override values after defaults are loaded. The host
applies Development only for debug/development builds and Windows only on
Windows, in that order.

| File / directory | Section | Controls |
| --- | --- | --- |
| `appsettings.json` | `Cryptography`, `Persistence`, `SyncScheduler`, `Cora`, `Carousel` | Custody AES-GCM/PBKDF2, on-device DB name, sync dispatch, Cora copy, carousel layout |
| `challenge-pass/` | `ChallengePass` | Installed session suite selection (non-secret identifiers only) |
| `network/` | `Network` | Product API and WebSocket endpoints by environment |
| `sonar/` | server quality gate | New-code quality thresholds and project assignment contract |

Development-only recipient seeds live in the Development overlay. Production
defaults intentionally bind an empty `Persistence:DefaultRecipients` list.
Never place secrets, tokens, production certificate pins, mnemonics, or
customer banking coordinates in these files. Invalid required security,
persistence, or suite values must fail options validation during startup.
