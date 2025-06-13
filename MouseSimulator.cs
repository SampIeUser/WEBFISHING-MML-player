
using System.Runtime.InteropServices;


namespace SV_WEBFISHING_GuitarMIDI
{
    internal class MouseSimulator
    {        
            [DllImport("user32.dll", SetLastError = true)]
            private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

            private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            private const uint MOUSEEVENTF_LEFTUP = 0x0004;

            public static void Click(Point clickPoint)
            {
                // Получаем текущие координаты курсора
                Point pos = clickPoint;
                Cursor.Position = pos;
                uint x = (uint)pos.X;
                uint y = (uint)pos.Y;

                // Симулируем нажатие и отпускание левой кнопки мыши
                mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0); // Нажимаем левую кнопку
                mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);   // Отпускаем левую кнопку
            }        
    }



}
