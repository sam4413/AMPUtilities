# AMPUtilities
AMPUtilities is a required plugin for the CubeCoders Space Engineers Torch template for AMP. If you are running Space Engineers Torch using CubeCoders AMP, please do not remove this plugin.

## What it does
This plugin essentially allows the torch server that is running in NoGUI mode to have a typeable console, therefore being able to send commands via the chat window. It also patches an issue where if the server crashes or restarts, it creates a zombie process, making AMP loose track of what server it is. Instead, if the server restarts or crashes for any reason, it will gracefully end the process.

To send commands simply type ! and any command that follows. (Example: `!whitelist add sam`)

To send a normal message, just type out what you want sent and the server will send it to the game.

