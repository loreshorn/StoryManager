# Story Manager
Windows desktop program for downloading, organizing, and viewing your favorite stories from [Literotica](https://literotica.com/stories/). Free and open-source, available under [MIT license](https://github.com/git/git-scm.com/blob/main/MIT-LICENSE.txt).

[Download the latest .exe here](https://github.com/Videogamers0/StoryManager/releases)

## Features

- Download and save stories for offline viewing

  ![Story Downloader1](https://github.com/Videogamers0/StoryManager/assets/9426230/79fb0be0-88e6-42bd-874b-e9cb034b612a)

  Paste a link into the textbox and click **Download** to retrieve a single story. Or paste a link to an author's submissions page and click **Retrieve stories by Author** to batch-download many   stories by the same author.

  ![Story Downloader2](https://github.com/Videogamers0/StoryManager/assets/9426230/66139d16-cae8-4e5e-b8e3-f824a9fda2a7)

  *StoryManager* automatically retrieves all pages of all chapters when you download a story, and saves the story content to a single combined file.

- Built-in viewer

  Downloaded stories are saved as a .html file so you can read them by opening the .html file with your favorite browser. Or, if you prefer, view the story directly within *StoryManager*.

  ![MainWindow1](https://github.com/Videogamers0/StoryManager/assets/9426230/cc173dd5-fb41-4853-958a-79ff47ac786c)

  The built-in viewer has a handful of minor usability features such as settings to change the font size, text color, or background color. Highlight a comma-separated list of your favorite keywords. Jump to specific pages or chapters. *StoryManager* also attempts to save your scroll position so you can re-visit a story later from where you left off (although the scroll position is dependent on the width of the viewer, so if you resize the window it won't be retained)

- All your stories in one convenient place

  The sidebar on the left contains all the stories you've downloaded, grouped by the author. Click to select one and load it. Customize what data appears from the filtering dropdown.

  ![Stories1](https://github.com/Videogamers0/StoryManager/assets/9426230/67a4b8f9-9488-470a-90a8-e3afea2756b0)

- Keep it organized

  Rate stories from 1 to 5 stars, mark as read or unread, add or remove to favorites, add your own notes to remember what a story was about, filter the list based on various criteria like word count, download date, rating, etc.

- Multiple libraries on one machine

  Each stories folder has its own `settings.json` (favorites/ratings/notes/history/theme/etc.), so different folders are completely independent libraries. Switching folders from the Settings dialog saves the current library, swaps in the new folder's settings, and reloads — all in-process, no restart required. A startup progress dialog shows the per-story load progress and a completion notification; the same dialog appears when you switch libraries.

- Built-in search

  Search for stories from directly within *StoryManager* by expanding the search settings at the bottom of the window.

  ![Search1](https://github.com/Videogamers0/StoryManager/assets/9426230/527e2f36-dd3a-4b4b-aed6-e1fcac41fcdd)

## FAQS

- Is this Windows-only?
  - Yes. It targets `net7.0-windows` instead of `net7.0` because it uses WPF.
- Where are the stories saved?
  - *%MyDocuments%\StoryManager\Stories\Literotica\{AuthorName}\{StoryTitle}*
    - This is usually something like *"C:\Users\{Username}\Documents\StoryManager\Stories\Literotica\"*
  - Stories are saved as both a .html file and as a .txt file. Some stories are better when viewed from the .html files.
  - You can change the default directory from the settings dialog (accessed from a button in the top-left corner of the program window)
- Where are the program's settings saved?
  - As of version 1.0.11.0, `settings.json` lives **inside the stories folder** (alongside the rotating `settings-bak1.json` … `settings-bak5.json` backups). Copy the stories folder to another drive or another machine and your favorites/ratings/notes/history come with it. Switching the stories folder via Settings automatically swaps in that folder's `settings.json`.
  - A small bootstrap pointer file at *%MyDocuments%\StoryManager\stories-folder.txt* records the absolute path of the stories folder so the program knows where to look on launch. It contains a single line of text and nothing else.
  - On first launch after upgrading from 1.0.10.0 or earlier, *StoryManager* automatically copies the previous *%MyDocuments%\StoryManager\settings.json* (and any backups) into the stories folder. The legacy files are left in place so you can roll back manually if anything looks wrong.
- What happens when I switch the stories folder mid-session?
  - *StoryManager* saves the current library's settings into the **old** folder, drops every piece of in-memory state that referenced the old library (loaded stories, author groups, back/forward history, recent list, the currently-selected story, the unsaved-warning exclusions), reloads `settings.json` from the **new** folder, reapplies the new library's theme / font / colors / sort / group / display flags, and runs a fresh story-load — all without restarting the process. The result is functionally identical to relaunching the program with a new bootstrap pointer.
  - During the swap a progress dialog shows the same per-story progress and "Done" notification as a fresh startup. The same dialog also appears at exit during settings save and auto-closes when the save completes.
- Does it work with illustrated stories?
  - Yes, but images are currently stored as links instead of saved to local files. If you open an illustrated story, the images will be retrieved from literotica's servers, requiring an internet connection. Maybe I'll fix this limitation at some point if anyone actually cares enough.
- Is it slow / does it crash on startup with a large library?
  - It shouldn't, as of version 1.0.11.0. Earlier versions cached every story's metadata inside a single `settings.json` file, which could grow to hundreds of megabytes for users with thousands of stories and crash the program on startup. Story metadata is now read from each story's per-folder `story-metadata.json` file instead, the sidebar list now uses UI virtualization (so author-grouped lists no longer instantiate a visual element per story), the stories collection is bulk-loaded with a single change notification, the search box debounces filtering as you type, and saving settings on close runs off the UI thread.
  - Existing `settings.json` files from earlier versions are still supported — the obsolete `StoryMetadata` field is silently ignored on load and dropped the next time settings are saved.
- I lost my settings / my favorites and ratings disappeared. Can I recover them?
  - Probably yes. *StoryManager* keeps up to 5 rolling backups of `settings.json` (created at most once per hour) inside your stories folder, named `settings-bak1.json` through `settings-bak5.json`.
  - To restore one, close *StoryManager*, copy the backup over `settings.json` (in the stories folder), and re-launch.
  - If the program ever finds a `settings.json` that it can't parse (e.g. partial write from a crash, manual edit gone wrong), it renames the unreadable file to *settings-corrupt-{yyyyMMdd-HHmmss}.json* in the same directory and starts with defaults instead of overwriting the bad file. You can recover content from the quarantined file the same way.
  - As of version 1.0.11.0, *StoryManager* will also no longer overwrite your settings file with empty data if you close the window before the story library has finished loading, and it writes settings atomically (via a sibling `.tmp` file) so a crash mid-write can no longer truncate `settings.json`.
  - If the configured stories folder is missing or unreachable at launch (e.g. you copied a `settings.json` from another machine, or the drive is offline), *StoryManager* now shows a *"Stories folder not found"* warning and skips loading. While this warning is active, your saved per-story metadata (favorites, ratings, notes, read-state, etc.) is preserved verbatim — the program will not overwrite it on close. Reconnect the drive (or update the stories folder via Settings) and re-launch to restore them.
