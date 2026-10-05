# Руководство пользователя: Universal Hex Grid & Targeting Engine (`com.neymanoff.hex-grid`)

> Универсальный высокопроизводительный модуль гексагональной сетки, A* поиска пути, пространственного таргетинга способностей и тактических формаций для тактических и пошаговых RPG на Unity.

---

## 1. Архитектура и принципы модуля

Модуль спроектирован по стандарту **Clean Architecture** с разделением на два независимых слоя:

1. **Pure C# Headless Core (`Neymanoff.HexGrid.Core`)**:
   * Не зависит от `UnityEngine.dll` (`noEngineReferences: true`).
   * Может выполняться в любом .NET-контексте (выделенный сервер, симуляция без графики, юнит-тесты).
   * Содержит: `HexCoord` (осевые $Q, R$ и кубические $S$ координаты с целочисленным вращением на $60^\circ$ шаги), `HexPathfinder` (A* поиск пути с правилами проходимости `ITraversalRule`), `HexFloodFill` (Dijkstra расчёт зоны хода по очкам действий AP), `HexOccupancyMap<T>` (пространственный хэш занятости ячеек $O(1)$), `HexTargetResolver` (геометрические проекции способностей: конус $120^\circ$, линия, кольцо, взрыв) и `FormationPlanner` (расчёт координат отряда с произвольным смещением).
2. **Unity Presentation Layer (`Neymanoff.HexGrid.Unity`)**:
   * Построен на нативных тайлмапах Unity с правильной **Pointy-Top** геометрией ($\frac{\sqrt{3}}{2} \approx 0.8659766$).
   * Работает в **3D плоскости XZ** (горизонтальная земля, ось $Y$ направлена вверх).
   * Включает: спаунер 3D-декораций поверх 2D-земли (`HexGrid3DSpawner`), точки тактических формаций (`HexFormationAnchor`, `HexSpawnPoint`), ScriptableObject шаблонов расстановки (`FormationPatternSO`), конвертер в UI-координаты экрана (`HexUiLayoutConverter`, `HexFormationPreviewUI`, `HexFormationSelectorUI`), и плавное перемещение юнитов (`GridMover`).

---

## 2. Как протестировать модуль в текущем проекте

Все системы уже настроены в демонстрационной сцене `Assets/Scenes/SampleScene.unity`.

### Шаг 1. Запуск и проверка в Scene View (без PlayMode)
1. Откройте сцену `Assets/Scenes/SampleScene.unity`.
2. В окне **Hierarchy** раскройте контейнер `Tactical Spawners`:
   * Выберите **`Player Squad Anchor`**:
     * В Scene View отобразится **синий контур** формации (пресет «2 спереди, 3 сзади») и синяя стрелка направления взгляда отряда (на Восток, в сторону врага).
     * В инспекторе нажмите кнопку **«Snap to Nearest Cell Center»** — анкор автоматически выровняется по центру ближайшего гекса.
   * Выберите **`Enemy Squad Anchor`**:
     * Отобразится **красный контур** формации (пресет «3 спереди, 2 сзади») и красная стрелка (на Запад, в сторону игрока).
   * Выберите **`Neutral Spawn Point`**:
     * Отобразится **жёлтый контур** одиночной точки призыва нейтрального юнита / босса / сундука.

### Шаг 2. Проверка в режиме воспроизведения (PlayMode ▶)
1. Нажмите кнопку **Play ▶** в Unity Editor:
   * **3D-окружение**: `HexGrid3DSpawner` автоматически разместит 3D-модели препятствий и деревьев на соответствующих ячейках. При этом нарисованный 2D-арт травы и земли остаётся видимым под объектами, а основания моделей плотно прилегают к плоскости земли ($Y = 0$).
   * **UI выбора формаций (слева вверху)**: на панели отображаются кнопки:
     * `2-3 Standard Squad`
     * `3-2 Vanguard Squad`
     * `1-2-1 Diamond Squad`
     * `Line (4) Defense`
     * `Wedge Spearhead`
   * **UI превью отряда (слева внизу)**: нажимайте на разные кнопки формаций — сетка слотов в превью будет перестраиваться в реальном времени с сохранением гексагональных пропорций.
   * **Управление и перемещение юнита**:
     * Наведите курсор на сетку — отобразится контурная рамка ячейки под курсором.
     * Кликните **ЛКМ** на доступную ячейку — `GridMover` плавно переместит кубик героя по центрам гексов с расчётом пути через A*.
   * **Тестирование форм способностей (AoE Targeting)**:
     * Нажмите клавишу **`1`** — режим «Конус $120^\circ$» (подсвечиваются ячейки в направлении взгляда).
     * Нажмите клавишу **`2`** — режим «Кольцо» вокруг цели.
     * Нажмите клавишу **`3`** — режим «Линия» (луч от юнита).
     * Нажмите клавишу **`0`** — возврат в стандартный режим перемещения.

### Шаг 3. Запуск автоматических тестов (NUnit)
В верхнем меню Unity выберите **Window $\rightarrow$ General $\rightarrow$ Test Runner**:
1. Перейдите на вкладку **EditMode**.
2. Нажмите **Run All**.
3. Все **92 теста** должны успешно пройти (зелёные галочки, 0 ошибок, 0 предупреждений).

---

## 3. Как подключить модуль в другой проект

Модуль оформлен как автономный Unity Package Manager (UPM) пакет `com.neymanoff.hex-grid`.

### Вариант А. Подключение как локальный пакет (рекомендуется при разработке)
1. В целевом проекте (например, `LegendsLegacyOfLost`) откройте файл `Packages/manifest.json`.
2. В секцию `"dependencies"` добавьте относительный путь к папке пакета:
   ```json
   "dependencies": {
     "com.neymanoff.hex-grid": "file:../../Universal-Hex-Grid/Packages/com.neymanoff.hex-grid",
     ...
   }
   ```
3. Либо в Unity Editor: **Window $\rightarrow$ Package Manager $\rightarrow$ Кнопка «+» $\rightarrow$ Add package from disk...** и выберите файл `Packages/com.neymanoff.hex-grid/package.json`.

### Вариант Б. Подключение через Git URL
Если репозиторий опубликован на GitHub:
1. В Package Manager выберите **Add package from git URL...**.
2. Укажите URL с параметром пути к пакету:
   ```text
   https://github.com/neymanoff/Universal-Hex-Grid.git?path=Packages/com.neymanoff.hex-grid
   ```

### Требования к целевому проекту
* **Версия Unity**: Unity 6 (6000.x) или новее.
* **Зависимости Unity**:
  * `com.unity.inputsystem` (новая система ввода);
  * `com.unity.modules.tilemap` (нативные тайлмапы Unity);
  * `com.unity.textmeshpro` (для UI компонентов).

---

## 4. Интеграция в сцену с нуля в другом проекте

### Шаг 1. Создание базовой иерархии сетки за 1 клик
В верхнем меню Unity целевого проекта выберите:
> **GameObject $\rightarrow$ Hex Grid $\rightarrow$ Create Tactical Grid Setup**

Это автоматически создаст корректную структуру объектов:
```text
Hex Grid (с компонентом Grid, cellSize = (0.8659766, 1, 1), CellLayout = Hexagon Pointy-Top)
├── Walkable Tilemap (слой проходимой земли)
├── Obstacle Tilemap (слой камней, стен, деревьев)
└── Highlight Overlay (служебный слой контурной и сплошной подсветки)
```
На объекте `Hex Grid` уже преднастроены компоненты:
* `HexTilemapBridge` — связывает физические тайлмапы с математикой `HexCoord`.
* `TilemapHighlightOverlay` — отвечает за эффекты подсветки.
* `TilemapPointerPicker` — перехватывает клики и наведение мыши/тача.

### Шаг 2. Рисование карты уровня
1. Откройте окно **Tile Palette** (**Window $\rightarrow$ 2D $\rightarrow$ Tile Palette**).
2. Выберите тайлы земли и нарисуйте игровое поле на слое `Walkable Tilemap`.
3. Переключитесь на `Obstacle Tilemap` и нарисуйте непроходимые препятствия.

### Шаг 3. Спаун 3D-моделей препятствий (`HexGrid3DSpawner`)
Если вам нужно, чтобы вместо плоских 2D-тайлов на препятствиях стояли 3D-деревья, скалы или колонны:
1. Добавьте на `Hex Grid` компонент **`HexGrid3DSpawner`**.
2. В списке `Tile Mappings` укажите соответствие:
   * **Tile**: ваш тайл препятствия из палитры (например, тайл дерева).
   * **Prefab**: ваш 3D-префаб дерева со стандартным масштабом `(1, 1, 1)`.
   * **Scale**: коэффициент масштабирования под ячейку (например, `(0.5, 0.5, 0.5)`).
   * **RotationOffset**: желаемый угол доворота модели.
3. Оставьте флаг `Hide Tilemaps On Spawn = false`, чтобы красивая 2D-земля под деревьями не исчезала.
4. Включите `Align Bottom To Surface = true`, чтобы основания мешей автоматически выравнивались точно по поверхности земли ($Y = 0$).

### Шаг 4. Расстановка точек спауна отрядов
1. Создайте пустой GameObject `Player Squad Anchor`.
2. Добавьте компонент **`HexFormationAnchor`**.
3. Укажите:
   * **Faction**: `CellOwner.Player`.
   * **Formation Pattern**: создайте ассет формации (**Assets $\rightarrow$ Create $\rightarrow$ Hex Grid $\rightarrow$ Formation Pattern**) или выберите готовый пресет (например, `Formation_2_3`).
   * **Facing Direction Step**: $0$..$5$ ($0$ = Восток, $3$ = Запад).
4. Аналогично создайте `Enemy Squad Anchor` с фракцией `CellOwner.Enemy` и противоположным направлением.

---

## 5. Примеры использования C# API в коде ядра игры

Модуль `Universal-Hex-Grid` предоставляет чистые контракты для боевой системы и игрового ядра:

### А. Получение координат слотов для спауна отряда боевым менеджером
```csharp
using Neymanoff.HexGrid.Unity;
using UnityEngine;

public class BattleInitializer : MonoBehaviour
{
    [SerializeField] private HexFormationAnchor _playerAnchor;
    [SerializeField] private HexTilemapBridge _bridge;

    public void SpawnBattleSquad(HeroData[] heroes)
    {
        // Анкор сетки только сообщает правильные пространственные слоты:
        var slots = _playerAnchor.CalculateSlots(_bridge);

        for (int i = 0; i < Mathf.Min(heroes.Length, slots.Count); i++)
        {
            var slot = slots[i];
            
            // Боевой менеджер игры сам спаунит боевого юнита:
            GameObject heroUnit = Instantiate(heroes[i].Prefab, slot.WorldPosition, slot.Rotation);

            // Привязываем юнита к карте занятости ячеек:
            var occupant = heroUnit.GetComponent<GridOccupant>();
            occupant.Bind(slot.Coord, slot.Faction);
        }
    }
}
```

### Б. Поиск пути и перемещение по сетке (A*)
```csharp
using Neymanoff.HexGrid.Core;
using Neymanoff.HexGrid.Unity;
using System.Collections.Generic;
using UnityEngine;

public class UnitTurnController : MonoBehaviour
{
    [SerializeField] private HexTilemapBridge _bridge;
    [SerializeField] private GridMover _mover;

    public void MoveUnitTo(HexCoord targetCoord)
    {
        HexCoord currentCoord = _bridge.WorldToHex(transform.position);

        // Поиск пути с учётом препятствий:
        List<HexCoord> path = HexPathfinder.FindPath(currentCoord, targetCoord, _bridge);

        if (path != null && path.Count > 0)
        {
            // Перевод пути в мировые координаты и плавное движение:
            List<Vector3> worldWaypoints = _bridge.HexPathToWorld(path);
            _mover.FollowPath(worldWaypoints, onComplete: () =>
            {
                Debug.Log("Юнит прибыл в целевую ячейку!");
            });
        }
    }
}
```

### В. Расчёт зоны хода по очкам действий (AP / Dijkstra)
```csharp
using Neymanoff.HexGrid.Core;
using Neymanoff.HexGrid.Unity;
using System.Collections.Generic;

public class ActionPointService
{
    public HashSet<HexCoord> GetMovementRange(HexCoord start, int availableAP, ITraversalRule rule)
    {
        // Возвращает все ячейки, достижимые в рамках бюджета AP:
        return HexFloodFill.GetReachableCells(start, availableAP, rule);
    }
}
```

### Г. Расчёт зоны поражения заклинания (AoE Targeting)
```csharp
using Neymanoff.HexGrid.Core;
using System.Collections.Generic;

public class SpellTargeting
{
    public List<HexCoord> CalculateConeDamage(HexCoord casterPos, HexCoord targetPos, int range)
    {
        // 120-градусный конус в сторону цели:
        return HexTargetResolver.Resolve(
            casterPos,
            targetPos,
            TargetShape.Cone,
            range: range,
            radius: 2);
    }
}
```

---

## 6. Решение типовых проблем (Troubleshooting)

| Проблема | Причина | Решение |
| :--- | :--- | :--- |
| **Сетка выглядит сплюснутой или ячейки перекрывают друг друга** | Неверный размер ячейки в компоненте `Grid`. | Убедитесь, что `Grid.cellSize` строго равен `(0.8659766, 1, 1)` ($\frac{\sqrt{3}}{2} \approx 0.8660254$), а `CellLayout` установлен в `Hexagon`. |
| **Юниты «утопают» в земле или висят в воздухе** | Не настроен вертикальный оффсет. | В инспекторе `HexTilemapBridge` задайте `UnitHeightOffset = 0.3` (или высоту пивота модели). В `HexGrid3DSpawner` включите флаг `AlignBottomToSurface`. |
| **Клик мыши не выбирает ячейку** | Камера смотрит под углом, или не настроен Raycast Plane. | В `TilemapPointerPicker` укажите целевую камеру и убедитесь, что плоскость `PlaneOrientation` выставлена в `XZ (Horizontal Ground)` с высотой $0.01$. |
| **При спауне 3D-деревьев пропадает земля под ними** | Включен флаг скрытия тайлмапов. | В инспекторе `HexGrid3DSpawner` снимите галочку `Hide Tilemaps On Spawn` (установите `false`). |
| **Контуры формаций не видны в Scene View** | Отключены Gizmos в окне сцены. | В верхнем правом углу окна Scene включите показ Gizmos и выберите объект с `HexFormationAnchor`. |
