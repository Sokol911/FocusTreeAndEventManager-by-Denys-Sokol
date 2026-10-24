<p align="center"><img src="images/app.png"></>

# FocusTreeAndEventManager
Focus tree and Event manager for Heart of Iron IV

## Credits / Авторство
**EN** — This project is a fork of [pongmadee/FocusTreeAndEventManager](https://github.com/pongmadee/FocusTreeAndEventManager), which is itself a fork of [Minivera/FocusTreeAndEventManager](https://github.com/Minivera/FocusTreeAndEventManager) (the original author). All credit for the original application belongs to them. The original license notice (`FocusTreeManager/Licence.txt`) and copyright line are kept unchanged. This fork is maintained by [Denys Sokol](https://github.com/Sokol911) and is not affiliated with or endorsed by the original authors.

**UK** — Цей проєкт — форк [pongmadee/FocusTreeAndEventManager](https://github.com/pongmadee/FocusTreeAndEventManager), який у свою чергу є форком [Minivera/FocusTreeAndEventManager](https://github.com/Minivera/FocusTreeAndEventManager) (першоджерело). Уся заслуга за оригінальну програму належить їхнім авторам. Оригінальна ліцензія (`FocusTreeManager/Licence.txt`) і рядок про авторські права залишені без змін. Форк підтримує [Denys Sokol](https://github.com/Sokol911); він не пов'язаний з оригінальними авторами та не схвалений ними.

### Changes in this fork / Зміни у цьому форку
- Faster focus tree editor, zoom of the grid (Ctrl + wheel, Ctrl+0), "zoom to fit" / швидший редактор дерева фокусів, масштабування сітки
- Pan the focus view by holding the left mouse button, Ctrl + drag selection box, edge auto-scroll while dragging / перетягування вигляду лівою кнопкою, рамка виділення, автопрокрутка біля краю
- Tree validator (loops, duplicates, useless links) / перевірка дерева на помилки
- Autosave with recovery of the project after a crash / автозбереження та відновлення проєкту після збою
- Fixed Ctrl+Z crash, wrong working directory and missing tutorial locale / виправлено збій Ctrl+Z, робочу теку, відсутню локаль туторіалу
- The image chooser opens instantly (virtualized list) / вікно вибору зображення відкривається миттєво
- Languages in alphabetical order, tidier texts and button captions / мови за алфавітом, охайніші тексти й кнопки
- Full translations (UI + tutorial): Español, Русский, Українська, Lietuvių, Suomi, Қазақша; French title marks the fork / повні переклади інтерфейсу й туторіалу

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
