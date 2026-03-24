# Luzart Property Attributes System

H? th?ng Property Attributes m? r?ng cho Unity Inspector, cho phép t?o UI m?nh m? và linh ho?t.

## Các Attributes Có S?n

### 1. ShowInInspectorAttribute
**Ch?c n?ng**: Hi?n th? b?t k? field, property, ho?c method nào trong Inspector, k? c? private members.

**S? d?ng**:
```csharp
[ShowInInspector] // S? d?ng tên field làm label
private int _score = 100;

[ShowInInspector("Custom Label")] // Label tùy ch?nh
private float _health = 75f;

[ShowInInspector("Read Only Field", true)] // Read-only
private Vector3 _position;

[ShowInInspector("Player Status")] // Method return value
private string GetStatus() => "Alive";

[ShowInInspector("Health %")] // Property
public float HealthPercent => _health / 100f;
```

### 2. ButtonAttribute - ? ENHANCED VERSION ?
**Ch?c n?ng**: T?o button trong Inspector ?? g?i method v?i **??y ?? support cho parameters**.

**Tính n?ng m?i**:
- ? **Automatic Parameter UI Generation**: T? ??ng t?o input fields cho t?t c? parameters
- ? **Foldable Parameter Section**: Có th? thu g?n/m? r?ng parameters 
- ? **Support Multiple Data Types**: int, float, bool, string, Vector2, Vector3, Color, Enums, UnityEngine.Object
- ? **Parameter Value Persistence**: Giá tr? parameters ???c l?u gi? gi?a các l?n s? d?ng
- ? **Works with Private Methods**: Ho?t ??ng v?i c? public và private methods

**S? d?ng**:
```csharp
[Button] // S? d?ng tên method làm text
public void ResetPlayer() { }

[Button("Custom Button Text")] // Text tùy ch?nh
public void DoSomething() { }

[Button("Large Button", ButtonSize.Large)] // Size tùy ch?nh
public void ImportantAction() { }

// ? NEW: Methods v?i parameters s? t? ??ng generate UI
[Button("Attack Enemy")]
public void Attack(int damage, string weaponType, bool isCritical)
{
    Debug.Log($"Attacked with {weaponType} for {damage} damage. Critical: {isCritical}");
}

[Button("Move Character")]
public void MoveCharacter(Vector3 position, float speed)
{
    Debug.Log($"Moving to {position} at speed {speed}");
}

[Button("Change Color")]
public void ChangeColor(Color newColor, bool applyToChildren)
{
    Debug.Log($"Color changed to {newColor}");
}

[Button("Set Player Stats")]
public void SetPlayerStats(string playerName, int level, float health, bool isActive)
{
    Debug.Log($"Player: {playerName}, Level: {level}, Health: {health}, Active: {isActive}");
}

[Button("Test Enum")] 
public void TestEnum(KeyCode keyCode)
{
    Debug.Log($"Key selected: {keyCode}");
}

[Button("Reference Objects")]
public void TestReferences(GameObject target, Transform transform)
{
    // Drag and drop objects from hierarchy
}

[Button("Private Action")] // Ho?t ??ng v?i private methods
private void PrivateMethodWithParams(int value, bool flag) { }
```

**Button Sizes**: `Small`, `Medium`, `Large`

**Supported Parameter Types**:
- ? `int` - Integer field
- ? `float` - Float field  
- ? `bool` - Toggle checkbox
- ? `string` - Text field
- ? `Vector2` - Vector2 field
- ? `Vector3` - Vector3 field
- ? `Color` - Color picker
- ? `Enums` - Dropdown selection
- ? `UnityEngine.Object` - Object reference field (drag & drop)
- ? **Inheritance Support**: B?t k? class nào inherit t? UnityEngine.Object

### 3. ReadOnlyAttribute
**Ch?c n?ng**: Làm field ch? ??c trong Inspector.

**S? d?ng**:
```csharp
[ReadOnly]
public float currentHealth = 100f;
```

### 4. InfoBoxAttribute
**Ch?c n?ng**: Hi?n th? h?p thông tin v?i các lo?i thông báo khác nhau.

**S? d?ng**:
```csharp
[InfoBox("This is information")]
[InfoBox("This is a warning", InfoBoxType.Warning)]
[InfoBox("This is an error", InfoBoxType.Error)]
public string someField;
```

### 5. ProgressBarAttribute
**Ch?c n?ng**: Hi?n th? thanh ti?n trình cho numeric values.

**S? d?ng**:
```csharp
[ProgressBar] // 0-1 range, default settings
public float normalizedHealth = 0.75f;

[ProgressBar("Health", 0f, 100f)] // Custom range
public float health = 75f;

[ProgressBar("XP", 0f, 1000f, false)] // Hide value text
public float experience = 250f;
```

### 6. DropdownAttribute - ? ENHANCED VERSION ?
**Ch?c n?ng**: T?o dropdown selection cho string, int, ho?c float values v?i **support cho custom display names**.

**? FIXED**: Gi?i quy?t v?n ?? v?i array syntax trong attributes!

**S? d?ng**:
```csharp
// Basic dropdown (simple values)
[Dropdown("Option1", "Option2", "Option3")]
public string selectedOption = "Option1";

[Dropdown(1, 5, 10, 25, 50)]
public int quantity = 5;

// ? NEW: DropdownNamed v?i custom display names
[DropdownNamed("1|Very Slow", "5|Slow", "10|Normal", "25|Fast", "50|Very Fast")]
public int gameSpeed = 10; // Hi?n th? "Normal" nh?ng value = 10

[DropdownNamed("warrior|Warrior Class", "mage|Mage Class", "archer|Archer Class")]
public string characterClass = "warrior"; // Hi?n th? "Warrior Class" nh?ng value = "warrior"

[DropdownNamed("0.5|Half Speed", "1.0|Normal", "1.5|Fast", "2.0|Double")]
public float animationSpeed = 1.0f; // Support c? float values!

// Syntax: "value|display_name"
// - Ph?n tr??c | là giá tr? th?c
// - Ph?n sau | là text hi?n th?
```

**Supported Types**: `string`, `int`, `float`

### 7. SliderAttribute  
**Ch?c n?ng**: T?o slider cho numeric values (t??ng t? Unity's Range attribute nh?ng v?i tên mô t? h?n).

**S? d?ng**:
```csharp
[Slider(0f, 10f)]
public float volume = 5f;

[Slider(1, 100)]
public int level = 1;
```

### 8. Conditional Attributes
**Ch?c n?ng**: Hi?n th?/?n/enable/disable fields d?a trên conditions.

**S? d?ng**:
```csharp
public bool showAdvanced = false;

[ShowIf("showAdvanced", true)]
public float advancedSetting = 1.0f;

[HideIf("showAdvanced", true)]
public string basicSetting = "Basic";

[EnableIf("showAdvanced", true)]
public bool enabledWhenAdvanced = false;

[DisableIf("level", 1)]
public string disabledWhenLevelOne = "Locked";

// Multiple conditions
[ShowIfAny("condition1", true, "condition2", false)]
public float showIfEither;

[ShowIfAll("condition1", true, "condition2", true)]
public float showIfBoth;
```

## Ví D? Hoàn Ch?nh - Enhanced Features

```csharp
using UnityEngine;
using Luzart;

public class PlayerController : MonoBehaviour
{
    [Header("Public Stats")]
    public string playerName = "Player";
    
    [ReadOnly]
    public float currentHealth = 100f;
    
    [Header("Dropdown Examples")]
    [InfoBox("? NEW: DropdownNamed v?i custom display names", InfoBoxType.Info)]
    
    [DropdownNamed("warrior|?? Warrior", "mage|?? Mage", "archer|?? Archer", "rogue|??? Rogue")]
    public string playerClass = "warrior";
    
    [DropdownNamed("1|?? Very Slow", "5|?? Slow", "10|?? Normal", "25|????? Fast", "50|??????? Lightning")]
    public int gameSpeed = 10;
    
    [DropdownNamed("0.5|Half", "1.0|Normal", "1.5|Fast", "2.0|Double", "3.0|Triple")]
    public float animationMultiplier = 1.0f;
    
    [Header("Enhanced Button Actions")]
    [InfoBox("These buttons demonstrate the new parameter support", InfoBoxType.Info)]
    
    // Enhanced button v?i parameters
    [Button("Set Game Settings")]
    public void SetGameSettings(int newSpeed, string newClass, float newMultiplier, bool resetHealth)
    {
        gameSpeed = newSpeed;
        playerClass = newClass;
        animationMultiplier = newMultiplier;
        
        if (resetHealth)
        {
            currentHealth = 100f;
        }
        
        Debug.Log($"Settings updated - Speed: {newSpeed}, Class: {newClass}, Multiplier: {newMultiplier}");
    }
    
    [Button("Detailed Attack")]
    public void DetailedAttack(int damage, string weaponType, Color effectColor, bool playSound)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"?? Attacked with {weaponType} for {damage} damage!");
        Debug.Log($"Effect Color: {effectColor}, Play Sound: {playSound}");
        Debug.Log($"Health remaining: {currentHealth}");
    }
    
    [Header("Debug Info")]
    [ShowInInspector("Game Speed Display")]
    private string GetGameSpeedDisplay()
    {
        return gameSpeed switch
        {
            1 => "?? Very Slow",
            5 => "?? Slow",
            10 => "?? Normal", 
            25 => "????? Fast",
            50 => "??????? Lightning",
            _ => $"Custom ({gameSpeed})"
        };
    }
    
    [ShowInInspector("Class Display")]
    private string GetClassDisplay()
    {
        return playerClass switch
        {
            "warrior" => "?? Warrior",
            "mage" => "?? Mage", 
            "archer" => "?? Archer",
            "rogue" => "??? Rogue",
            _ => $"Unknown ({playerClass})"
        };
    }
}
```

## ?? Technical Implementation

### Dropdown Enhancement
**V?n ?? c?**: Không th? s? d?ng `new int[] {...}` trong attribute syntax
**Gi?i pháp**: T?o `DropdownNamedAttribute` v?i string parsing

```csharp
// ? Không work (C# limitation)  
[Dropdown(new int[] { 1, 5, 10 }, new string[] { "Slow", "Normal", "Fast" })]

// ? Work v?i DropdownNamed
[DropdownNamed("1|Slow", "5|Normal", "10|Fast")]
```

### Button Parameter Enhancement  
- Automatic UI generation cho t?t c? parameter types
- Foldout interface cho methods có parameters
- Parameter value persistence across Inspector refreshes
- Support cho Unity Object drag & drop

## L?u Ý

1. **ShowInInspectorAttribute** ho?t ??ng v?i m?i access modifier (public, private, protected)
2. **ButtonAttribute** có th? s? d?ng v?i c? public và private methods
3. **Enhanced Button Parameters**: 
   - Automatically generates appropriate UI controls for each parameter type
   - Parameter values persist between Inspector refreshes
   - Supports foldout UI for methods with parameters
   - Works with Unity's Undo system
4. **Enhanced Dropdown**:
   - `DropdownAttribute`: Basic dropdown v?i simple values
   - `DropdownNamedAttribute`: Advanced dropdown v?i custom display names
   - Support cho string, int, và float types
   - Syntax: `"value|display_name"`
5. Các conditional attributes ch? ho?t ??ng v?i public fields
6. H? th?ng s? d?ng Custom Editor, vì v?y s? override default inspector behavior
7. T?t c? changes ???c record v?i Undo system c?a Unity

## File Structure

```
Assets/_GameLuzart/Utility/Script/
??? Editor_PropertyDrawer/
?   ??? ConditionalAttributes.cs          # Attribute definitions including ButtonAttribute
?   ??? ExtendedAttributes.cs             # Enhanced attribute definitions with DropdownNamed
??? Editor/PropertyDrawer/
?   ??? ShowInInspectorEditor.cs          # Main custom editor v?i button parameter support
?   ??? ButtonPropertyDrawer.cs           # Enhanced button property drawer
?   ??? DropdownPropertyDrawer.cs         # Enhanced dropdown v?i DropdownNamed support
?   ??? ReadOnlyPropertyDrawer.cs         # ReadOnly property drawer
?   ??? InfoBoxPropertyDrawer.cs          # InfoBox property drawer
?   ??? ProgressBarPropertyDrawer.cs      # ProgressBar property drawer
?   ??? SliderPropertyDrawer.cs           # Slider property drawer
??? Examples/
    ??? PropertyAttributeExample.cs       # Comprehensive examples
    ??? ButtonParameterExample.cs         # Focused button parameter examples  
    ??? DropdownTestExample.cs            # Dropdown testing and examples