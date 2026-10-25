<p align="center"><img src="images/app.png"></>

# FocusTreeAndEventManager
Focus tree and Event manager for Heart of Iron IV

## Credits
This project is a fork of [pongmadee/FocusTreeAndEventManager](https://github.com/pongmadee/FocusTreeAndEventManager), which is itself a fork of [Minivera/FocusTreeAndEventManager](https://github.com/Minivera/FocusTreeAndEventManager) (the original author). All credit for the original application belongs to them. The original license notice (`FocusTreeManager/Licence.txt`) and copyright are retained in full; this fork's author is added alongside them (in `Licence.txt` and the assembly copyright). This fork is maintained by [Denys Sokol](https://github.com/Sokol911) and is not affiliated with or endorsed by the original authors.

### Changes in this fork
- Faster focus tree editor, zoom of the grid (Ctrl + wheel, Ctrl+0), "zoom to fit"
- Pan the focus view by holding the mouse wheel (middle button), drag selection box with the left button, edge auto-scroll while dragging
- Real-time warnings in the focus edit panel: spaces or invalid characters in ids, duplicate ids (also case-only and across trees), ids starting with a digit, empty ids, two foci on the same tile
- Tree validator (loops, duplicates, useless links)
- Autosave with recovery of the project after a crash
- Fixed Ctrl+Z crash, wrong working directory and missing tutorial locale
- The image chooser opens instantly (virtualized list)
- Languages in alphabetical order, tidier texts and button captions
- Full translations (UI + tutorial): Español, Русский, Українська, Lietuvių, Suomi, Қазақша; French title marks the fork

![GitHub release (latest by date)](https://img.shields.io/github/v/release/pongmadee/FocusTreeAndEventManager) ![GitHub top language](https://img.shields.io/github/languages/top/pongmadee/FocusTreeAndEventManager)
#### [Download the lastest release](https://github.com/pongmadee/FocusTreeAndEventManager/releases)

## Fixed and change information
- Fixed localization files save in UTF-8 with BOM format
- Fixed event files save in UTF-8 with BOM format
- Fixed localization export file name pattern = {name}_{language_set}.yml eg. 'test_l_english.yml'
- Fixed focus image and button, interface layout in Edit focus panel & Select focus icon window
- Fixed AsyncImageLoader load event image
- Fixed update localization text to data grid when modified description script
- Fixed Scripter block incorrect condition title (And / Or )
- Fixed selected event image not display on change image event
- Remember file project path for next open or export project
- Change cost variable type form double to int reference to wiki documents
- Compatible with 1.7.x


### Important :
> When you create a new localization file please set language define to export file name pattern = {name}_{language_set}.yml eg. 'test_l_english.yml'.

<img src="images/demo_eng_set_loc_language.jpg" width="30%"> <img src="images/demo_01.PNG" width="30%">  <img src="images/demo_02.PNG" width="30%"> <img src="images/demo_03.PNG" width="30%"> <img src="images/demo_04.PNG" width="30%"> <img src="images/demo_05.PNG" width="30%">

## Other Tools
- [HOI4-QuickInsertGFX](https://github.com/pongmadee/HOI4-QuickInsertGFX) : This tool was created to work with FocusTreeAndEventManager, help to insert events pictures & goals/focus images data into the GFX files(eventpictures.gfx ,goals.gfx ,goals_shine.gfx) in the mod's interface directory.
