using UnityEngine;

/*
Три атрибута-подсказки для строковых полей, куда вписывается имя из настроек проекта.

Смысл один: тег, слой физики и sorting layer в разметке хранятся строками (данные переживают
переименование класса и читаются в диффе), но руками их набирать нельзя — опечатка тихо уводит
объект в Default. Атрибут превращает поле в выпадающий список, см. NameDropdownDrawers в Editor.

Держим их в одном файле: у каждого по одной строке тела, и по смыслу это один приём.
*/

// Тег GameObject: Floor, Platform, CameraBarrier…
public class TagNameAttribute : PropertyAttribute
{
}

// Слой физики: Floor, Walls, Platform, Ignore Raycast…
public class PhysicsLayerNameAttribute : PropertyAttribute
{
}

// Sorting layer спрайта.
public class SortingLayerNameAttribute : PropertyAttribute
{
}
