# No-device acceptance matrix for the beginner update wizard

All scenarios are simulated by unit tests and a non-writing seven-stage UI tutorial. These checks **cannot substitute for hardware acceptance**.

| Scenario | Required outcome |
| --- | --- |
| No ADB/Fastboot | Display official install/help URL, prevent firmware search and update |
| No Pixel / unauthorized / offline | Specific user-facing fix instructions, all writes blocked |
| More than one device/emulator | No device selected, block updates |
| Fastboot/Recovery at initial scan | Explain normal Android prerequisite |
| Battery low/unknown | Show charge instruction, do not enter Recovery |
| Pixel 8 shiba, sole compatible production build | Suggest for review, not install automatically |
| Pixel 8 shiba, two same-day regional builds | No automatic suggestion/download |
| Matching beta or different-device file | Exclude from production recommendation |
| Offline/cancelled or malicious download | No partial ZIP admitted, official SHA256 mismatch blocks |
| Valid cached ZIP | Verify hash again before trusting cache |
| Wrong OTA metadata / post timestamp downgrade | Block transition to Recovery/Sideload |
| Device swapped before Recovery | Block reboot |
| Device serial changed in sideload mode | Block transfer |
| Recovery incomplete or ADB returns error | No automatic restart, explain phone is authoritative |
| Post-reboot wrong Build Fingerprint | Never show installation success |
| Seven-stage demo without USB device | Show full workflow only; never populate device/write approval |
| Factory Image | Only checksum/read-only inspection, all writes unavailable |

Run `dotnet test tests/FlowAndroid.Core.Tests/FlowAndroid.Core.Tests.csproj -c Release` and follow up with `docs/MANUAL_ACCEPTANCE.md` on expendable supported Pixel hardware.
