# DOText Animation Usage Guide

## Tính n?ng m?i: DOText Animation cho TextMeshPro

### Cách s? d?ng:

1. **Thêm TweenAnimation component** vào GameObject có TextMeshProUGUI
2. **Ch?n Animation Type** = "DOText"
3. **C?u hình Text Options:**
   - **From Text**: Text hi?n th? ban ??u (có th? ?? tr?ng)
   - **To Text**: Text s? hi?n th? sau animation
   - **Rich Text Enabled**: Cho phép s? d?ng rich text markup
   - **Scramble Mode**: Hi?u ?ng scramble text (None, Uppercase, Lowercase, Numerals, Custom)
   - **Scramble Chars**: Ký t? custom cho scramble (khi mode = Custom)

### Base Options:
- **Show Ease/Hide Ease**: Curve animation
- **Duration**: Th?i gian animation
- **Start Delay**: Delay tr??c khi b?t ??u
- **Ignore Time Scale**: B? qua Time.timeScale

### Ví d? s? d?ng:
```csharp
// Show text animation
tweenAnimation.Show();

// Hide text animation  
tweenAnimation.Hide();

// Show v?i callback
tweenAnimation.Show(() => {
    Debug.Log("Animation completed!");
});
```

### Scramble Effects:
- **None**: Không có hi?u ?ng scramble
- **Uppercase**: Dùng ch? cái in hoa ng?u nhiên
- **Lowercase**: Dùng ch? cái th??ng ng?u nhiên  
- **Numerals**: Dùng s? ng?u nhiên
- **Custom**: Dùng ký t? custom t? ScrambleChars

### L?u ý:
- Component TextMeshProUGUI s? ???c t? ??ng thêm n?u ch?a có
- Có th? s? d?ng trong Loop animation
- H? tr? Rich Text nh? `<color=red>Text</color>`