# Runtime configuration

Configuration is separated by operational theme and embedded into Core as safe
defaults. The host applies the Development overlay only for debug/development
builds and the Windows overlay only on Windows, in that order.

| Directory | Section | Controls |
| --- | --- | --- |
| `security/` | `Cryptography` | Custody AES-GCM and PBKDF2 parameters |
| `dispatch/` | `SyncScheduler` | Sync concurrency and dispatch behavior |
| `persistence/` | `Persistence` | On-device database naming and initialization |
| `sonar/` | server quality gate | New-code quality thresholds and project assignment contract |
| `ui/` | `Cora`, `Carousel` | Cora copy and carousel layout defaults |

Development-only recipient seeds live in the Development overlay. Production
defaults intentionally bind an empty `Persistence:DefaultRecipients` list.
Never place secrets, tokens, production certificate pins, mnemonics, or
customer banking coordinates in these files. Invalid required values must fail
options validation during startup.
