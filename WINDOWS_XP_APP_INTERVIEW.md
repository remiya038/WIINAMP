# Windows XP-style App Interview Workflow

Use this workflow when the user says: "I want to make a Windows XP-style app."

Ask one question at a time. Do not repeat details already provided.

1. What is the application name?
2. What is its purpose and its three most important features?
3. What should appear on the main screen?
4. What icon image should be used for the EXE and taskbar?
5. Are a settings screen or collapsible panels required?
6. Does it need any Windows, file, or external-service integration?
7. What should the distributable EXE be named?

Defaults to propose when the user has no preference:

- Windows 11, single-file EXE.
- App folder: `projects/[app-name]/`.
- XP Luna Blue title bar.
- Taskbar title and EXE name match the application name.
- Build into the app's existing `publish/` folder and update the EXE in the app root.

When answers are complete, create a filled version of `WINDOWS_XP_APP_PROMPT.md`, show the implementation summary, then begin work only after the user confirms it.
