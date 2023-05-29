# APB Config Manager

After years of tediously switching between multiple APB configs manually, I decided to throw together this overcooked spaghetti of a codebase out of frustration in order to make managing and switching between configurations in APB a little bit smoother.

## Features

#### Creating a profile

Using APB Config Manager, you can create multiple configs without requiring multiple installs of APB by storing them as profiles. Each profile contains its own set of configuration files, which are automatically switched out when you run a specific profile.

#### Switching between profiles

To make switching between profiles as seamless as possible, you can create desktop shortcuts for each of your profiles. These shortcuts will automatically switch to the configuration files associated with the profile when double-clicked, allowing you to run the game with the desired configuration easily.

#### Editing a profile

To edit a config profile, simply select it from the list and run APB Advanced Launcher. You can then edit the configuration files as you normally would. Make sure you select the correct profile before you start editing, as you might unintentionally edit the wrong config otherwise.

#### Importing a profile

If you already have multiple installations of APB, each with its own config, APB Config Manager allows you to easily import them as profiles. Press "Import Profile...", navigate to the root directory of the APB installation you want to import, and the config files will be copied to a new config profile.

#### First-time startup

When you start APB Config Manager for the first time, it will prompt you to navigate to your primary APB installation directory. This directory is usually located in `C:\Program Files (x86)\Steam\steamapps\common\APB Reloaded`. Once you choose a valid directory, APB Config Manager will create a backup of the current config files. This backup profile is write-protected, meaning you cannot edit or remove it. This ensures that there is always a set of valid config files available for you to revert to in case something goes wrong.

#### When Murphy's Law kicks in

Refer to sections 7 and 8 of the [LICENSE file](LICENSE). ^-^

## Screenshots

![Picture of Application](https://i.imgur.com/ZxkrKrB.png)

---

<br>

This project is licensed under the Apache License 2.0. See the [LICENSE file](LICENSE) for more information.

<br>

---

### ***DISCLAIMER***

*I am in no way affiliated with Little Orbit or APB: Reloaded, and this software has not been officially approved. Although it should be perfectly legal in theory, you are using it at your own risk in practice. I am also not affiliated with the creator(s) of APB Advanced Launcher.*
