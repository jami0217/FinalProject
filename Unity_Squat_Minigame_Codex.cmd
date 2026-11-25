@echo off
REM =============================================================
REM Unity 2D Gym Game - Squat Minigame implementation brief
REM This .cmd is a TEXT SPECIFICATION for Codex. DO NOT EXECUTE IT.
REM Place this file at the Unity project root and ask Codex to READ it.
REM =============================================================
REM
REM [PROJECT CONTEXT]
REM Existing Unity 2D fitness game with character movement, gym scene,
REM equipment triggers, and potentially other minigames.
REM Recent teammate scene merge caused missing interaction references.
REM DO NOT overwrite scenes, existing player movement, or teammate content.
REM First inspect existing scripts, scene structure, prefabs, UI and Git status.
REM Avoid changing SampleScene.unity directly unless strictly necessary.
REM
REM [GOAL]
REM Build a Squat minigame inspired by a golf swing / timing power bar.
REM A pointer oscillates horizontally between the ends of a colored bar.
REM The player presses SPACE once to stop the pointer.
REM GREEN optimal region -> one successful squat immediately.
REM RED danger region -> exhausted state; repeatedly tap SPACE
REM to recover and finish exactly one squat.
REM A yellow/neutral region -> normal successful squat (or slower animation)
REM so every timing outcome is unambiguous.
REM
REM [SUGGESTED DEFAULT GAMEPLAY - INSPECT FIRST, MAKE CONFIGURABLE]
REM Bar: 0..1 normalized horizontal position; left and right red zones,
REM middle green optimal zone, intermediate yellow sections.
REM Example configurable ranges:
REM red: [0.00,0.15] and [0.85,1.00]
REM yellow: (0.15,0.40) and (0.60,0.85)
REM green: [0.40,0.60]
REM Pointer moves ping-pong at inspector-adjustable speed.
REM Press SPACE in green: Perfect +1 rep.
REM Press SPACE in yellow: Good +1 rep.
REM Press SPACE in red: enter Exhaustion state, pause pointer;
REM show '力竭！連按空白鍵恢復！' and a recovery progress bar.
REM Each NEW key-down counts as one mash (no holding to auto-complete).
REM Default 8 presses after entering exhaustion -> +1 squat rep.
REM No further presses affect the pointer until recovery ends.
REM After each rep show brief feedback, then restart pointer for next rep.
REM Default goal 5 squats, editable in Inspector.
REM On reaching goal show a completion panel, allow returning to gym
REM through project's existing scene/minigame manager (do not duplicate).
REM Optionally ESC returns to gym with a confirm dialog (no progress loss).
REM
REM [PLAYER EXPERIENCE / UI]
REM Use Unity Canvas, TextMeshPro if already installed, and Image UI.
REM Display title '深蹲挑戰', rep count '深蹲 0/5', colored timing bar,
REM pointer, '空白鍵：停止指針' instruction, feedback '完美/成功/力竭',
REM exhaustion recovery bar + '連按空白鍵' and completion panel.
REM Can implement bar as 5 adjacent Image segments (red-yellow-green-
REM yellow-red) with RectTransform pointer anchored in bar coordinates.
REM The pointer should visually reverse at boundaries; collision not needed.
REM Green and red thresholds must match displayed color segments exactly.
REM Interface scales for Game view via Canvas Scaler.
REM The squat character can initially use a simple placeholder vertical
REM crouch animation or UI feedback if there is no ready character animation.
REM Avoid requiring external paid assets.
REM
REM [ENGINEERING REQUIREMENTS]
REM Implement a clean state machine: Ready, Sweeping, Exhausted,
REM Feedback, Completed (and optional Paused).
REM Do not count multiple inputs on one frame, and prevent duplicate reps.
REM Prefer Time.deltaTime for pointer movement, with configurable speed.
REM Make bar thresholds, rep goal, mash target, feedback duration,
REM input key and acceleration configurable as serialized fields.
REM Respect current project's input system: inspect whether legacy Input
REM Manager or new Input System is active before choosing code/APIs.
REM Avoid class name clashes; namespace or clear class naming.
REM Keep scene switching compatible with existing project and Build Profiles.
REM Attach references safely; log useful warnings for missing UI links.
REM Do not change player controller, Git history, or unrelated objects.
REM
REM [INTEGRATION]
REM Find the existing squat-equipment object (one of three blocks) and its
REM 'press E' interaction. Reuse that trigger to open the Squat minigame.
REM If interaction is currently broken, explain missing references and
REM propose a minimal non-destructive fix; DO NOT silently rebuild whole gym.
REM Maintain original gym scene and player position when returning.
REM Record squat completion in existing progress manager if one exists.
REM If no existing manager, propose a minimal interface to implement later.
REM If scene editing is unsafe, give precise manual Unity setup instructions.
REM
REM [DELIVERABLES]
REM 1. First report what relevant assets/scripts/scenes currently exist
REM    and exactly which files you intend to create/change.
REM 2. Implement modular C# scripts for the timing bar / squat game.
REM 3. Explain step-by-step Unity Editor setup: Canvas, UI hierarchy,
REM    slider/pointer RectTransform, scene, Inspector fields, events,
REM    Build Profiles scene inclusion, and equipment trigger binding.
REM 4. Give test steps: green hit, yellow hit, red mash, boundaries,
REM    5-rep completion, scene return, and no regression in movement.
REM 5. Explicitly list anything still requiring manual setup.
REM
REM [SAFE WORKFLOW]
REM Backup scene files before ANY edits and check Git diff.
REM Never delete, overwrite, or replace existing scenes or prefabs.
REM Preserve teammate content. If unsure, first show plan and ask
REM for the minimum specific Unity Inspector step instead of guessing.
REM =============================================================
