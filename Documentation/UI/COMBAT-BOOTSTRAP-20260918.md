# Production bootstrap combat activation

The real MainScreenBootstrap.Build constructs the complete factory below an inactive root and enables it afterwards. BattleRuntime.Initialize previously attempted StartCoroutine while inactive; Unity rejected it, so the main screen stayed at battle-ready with empty health labels. Active synthetic combat fixtures did not exercise this path.

Initialize now builds resources and calls a guarded StartBattleWhenReady. OnEnable calls the same method after the real root activates. It checks initialization, active state, catalog/camera, missing-event failure and an existing coroutine before starting. OnDisable cancels actions, stops coroutines and clears the handle; reactivation restarts the same normal stage or accepted external encounter. Inactive external starts reject without consuming callback. The isolated renderer is disabled until its UI is active.

Both health labels use font best-fit (minimum18, maximum actual created font size) and vertical truncation for large scientific-notation values.

Two new full production PlayMode acceptance tests load the actual MainScreenAssets:
1. Build RuntimeMainScreenFactory under an inactive parent, assert no unexpected Unity errors and no premature combat, activate it and require normal wave/round, actual Animator basic damage and populated health labels; disable/reactivate and require a fresh progressing encounter.
2. Activate the real MainScreenBootstrap and run its actual Awake -> Build -> inactive factory -> root activation path; assert idempotent Build and one battle component, then require real damage and visible health labels.

These tests are authored for cloud Unity6000.3.8f1 Editor PlayMode. They have not been run locally. The previous106-test pass did not verify this production initialization path and must not be treated as startup acceptance.
