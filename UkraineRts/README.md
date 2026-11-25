# Frontline Sandbox (Unity 2D RTS sandbox)

Open this folder (`UkraineRts`) in Unity Hub (Unity 6 (6000.x); project last verified for compilation only), open `Assets/Scenes/Main.unity` and press Play.

All art, effects, sounds and UI are generated procedurally at runtime (`Assets/Scripts`), so there are no prefabs or art assets to import. `Assets/Resources/RTSAdditive.shader` is the additive sprite shader used for glows and fire.

Controls are listed on the in-game CONTROLS screen. The scene needs only a camera and the `Game` component; `Game` also bootstraps itself if the scene is empty.
