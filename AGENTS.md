# Agent Instructions

## Unity Project Lock Policy

- Never copy this Unity project to another directory to work around a project lock.
- Before starting Unity Editor, batch-mode tests, Play Mode automation, or a Unity build, check whether an existing Unity process has this project path open or whether the project lock indicates that it is occupied.
- If this project is already occupied by Unity, do not start another Unity instance or batch command. Pause the Unity-related task and report the lock to the user.
- Never force-terminate the user's Unity Editor or remove Unity lock files to bypass the lock.
- Read-only repository inspection and non-Unity analysis may continue while the project is occupied.
