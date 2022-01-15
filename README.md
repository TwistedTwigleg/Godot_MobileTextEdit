# Godot Mobile TextEdit

This repository contains a custom TextEdit like node that is designed to be similar to TextEdit, including with syntax highlighting,
but with performance that allows for editing large files on mobile devices.

This plugin is designed for **Godot 3.X** and **requires the Mono/C# version of Godot**. Also, the code here is very much a work in progress. Please use at your own risk.

### Features

* Expected text operations: Add and delete text, add new lines, etc
* Copy with Control-C or Command-C, Paste with Control-V or Command-V
* Working syntax highlighting
  * Similar to the syntax highlighting in the TextEdit node. Comes with optional GDScript highlighting by default
* Text navigation with arrow keys, change cursor position by clicking/touching text
* Text selection by holding shift and moving the cursor (arrow keys, click/touch) - operations should work as expected with selected text
* Fast performance - good for mobile and other performance constrained hardware
* Written in C# for increased performance
  * Helpful for text crawling for syntax highlighting and similar
  * Technically speaking, there is nothing in the code that would prevent it from being ported to GDScript - no C# specific features are used

Things-missing/Known-bugs:

* Does not support cursor movement on iOS by holding space bar
  * Need to see if it's possible to support this or not by seeing what (if any) input Godot gets while this is active
* Does not support arrow key navigation using Bluetooth keyboards on iOS
  * Need to see if it's possible to support this or not by seeing what (if any) input Godot gets while this is active
* Tabs seem to cause a strange offset at times, not sure what causes it just yet as it seems to only apply to some lines
* No undo/redo support.
  * Ideally this is something that would be added in the future to greatly increase usability, but it is a rather complicated issue.
  * I do not plan to add this currently.
* No multi-line syntax support (example "/*" and "*/" across multiple lines)
  * This is in part due to performance reasons, and in part due to complexity.
  * I do not plan to add this currently.

**The code in this repository is not actively supported, so please use at your own risk! While I am using this for my own project, I cannot dedicate support for fixing bugs, reviewing PRs, and more at this time.**

Thanks and enjoy the code!
