# How to use

## Basic usage

https://github.com/cstria0106/OneClickInventory/assets/11474150/0f8f1d61-c947-49cd-85a7-ac0e246ed96a

1.  Dress your avatar
    Dress your avatar. Use modular avatar or manual way.
    Organize your hierarchy like,

    ```
    Avatar
    ├─ Cloths (Inventory)
    │  ├─ Default (Item)
    │  ├─ Cute one (Item)
    │  ├─ Pretty one (Item)
    ├─ Hairs (Inventory)
    │  ├─ Long hair (Item)
    │  ├─ Short hair (Item)
    │  ├─ Cute hair (Item/Inventory)
    │     ├─ Red color (Item)
    │     ├─ Blue color (Item)
    ├─ Accessories (Inventory)
    │  ├─ Glasses (Item)
    │  ├─ Hat (Item)
    ```

2.  Setup inventory
    Select inventory (GameObject that includes items) and add Inventory component. And add Inventory component to each items too. Setup menu name, icon, or some other values in inspector.
3.  Test / Upload
    Click play button or upload to VRChat. Inventory animations and menu will be automatically generated.

## Add submenu

https://github.com/cstria0106/OneClickInventory/assets/11474150/264bd70d-6d17-4341-8122-9b5f3a9c9b7d

You can add submenu by nesting GameObject with Inventory component.
