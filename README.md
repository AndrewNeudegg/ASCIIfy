# C# ASCIIfy
Code converts a bitmap to ASCII art or a ASCII art rendered on a Bitmap. Accepts scaling and variations in the size of the regions sampled for the ASCII keys.

# How-To

```csharp

/*
*   bitmap: Input bitmap.
*   Font: Font for rendering the ASCII image.
*   Brush: Brush for rendering
*   Color: Colour of output text.
*   Rectangle: Represents only height and width elements, X and Y values are disregarded.
*   OutputScale: Output scale relative to input (input scale = 1).
*/
Bitmap output = new ASCIIfy(Bitmap bitmap, Font font, Color color, Rectangle pixelSizeRectangle,double outputScale);
string output = new ASCIIfy(Bitmap bitmap, Rectangle pixelSizeRectangle,double outputScale);

var ascii = new ASCIIfy();
string output = ascii.GetAsciiString(Bitmap bitmap, Rectangle pixelSizeRectangle,double outputScale)
Bitmap output = ascii.GetAsciiBitmap(Bitmap bitmap, Rectangle pixelSizeRectangle, Font font, Color color, double outputScale);

```
