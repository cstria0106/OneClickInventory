# How to use

## Basic usage

1.  Dress your avatar
    Dress your avatar. Use modular avatar or manual way.
    Organize your hierarchy like,

    ```
    Avatar
    ├─ Cloths
    │  ├─ Default
    │  ├─ Cute one
    │  ├─ Pretty one
    ├─ Hairs
    │  ├─ Long hair
    │  ├─ Short hair
    │  ├─ Cute hair
    ├─ Accessories
    │  ├─ Glasses
    │  ├─ Hat
    ```

2.  Setup closet
    Select closet (GameObject that includes cloths) and add Closet component. And add Closet component to each cloths too. Setup menu name, icon, or some other values in inspector.
3.  Test / Upload
    Click play button or upload to VRChat. Closet animations and menu will be automatically generated.

## Add submenu

You can add submenu by nesting GameObject with Closet component.
