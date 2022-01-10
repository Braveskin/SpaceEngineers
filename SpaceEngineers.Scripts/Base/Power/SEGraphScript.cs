using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEGraphScript { 

    public class Program : SEScriptTemplate {



        const double BIT_SPACING = 255.0 / 7.0;

        const char TRANS_FAKE_CHAR = '#';

        const string spacer = "\ue075\ue072\ue070";
        const string spacer2 = "\ue076\ue073\ue071";
        const string spacer4 = "\ue076\ue076\ue074\ue072";
        const string spacer8 = "\ue078\ue075\ue073";
        static string spacer178 = new string('\ue078', 25) + "\ue077\ue075\ue074\ue073\ue071";

        static string trans = TRANS_FAKE_CHAR.ToString();
        static string trans2 = new string(TRANS_FAKE_CHAR, 2);
        static string trans4 = new string(TRANS_FAKE_CHAR, 4);
        static string trans8 = new string(TRANS_FAKE_CHAR, 8);
        static string trans178 = new string(TRANS_FAKE_CHAR, 178);

        const int LCD_PX = 89; // Change this for more or less resolution.

        static Color3 COLOR_PRODUCED = new Color3(0, 255, 0);
        static Color3 COLOR_CONSUMED = new Color3(255, 0, 0);
        static Color3 COLOR_STORED = new Color3(0, 255, 255);

        IMyTextPanel _lcd;
        int[,] _colorArray;
        List<PowerData> _data;

        public Program() {
            _lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD Graph");
            _colorArray = new int[LCD_PX, LCD_PX];
            _data = new List<PowerData>();
        }

        void Main() {

            var producers = new List<IMyPowerProducer>();
            GridTerminalSystem.GetBlocksOfType(producers, b => b.CubeGrid == Me.CubeGrid && !(b is IMyBatteryBlock));

            var batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteries, b => b.CubeGrid == Me.CubeGrid);

            var storedPower = batteries.Sum(b => b.CurrentStoredPower);
            var producedPower = producers.Sum(p => p.CurrentOutput);
            var consumedPower = batteries.Sum(b => b.CurrentOutput);

            _data.Add(new PowerData(storedPower, producedPower, consumedPower));

            while (_data.Count > LCD_PX * 1.5f)
                _data.RemoveAt(0);

            _colorArray = new int[LCD_PX, LCD_PX];

            var x = 0;
            var height = 0;
            var lastHeight = 0;

            var maxStorage = batteries.Sum(b => b.MaxStoredPower);
            var maxProducedConsumed = Math.Max(_data.Max(d => d.Produced), _data.Max(d => d.Consumed));

            var lastData = default(PowerData);
            var color = default(Color3);
            var oldest = _data.Min(d => d.TimeStamp);
            var oldestAge = (float)(DateTime.Now - oldest).TotalSeconds;
            var ageRatio = 0f;

            foreach (var data in _data) {

                var age = data.GetAge();
                ageRatio = 1f - (age / oldestAge);

                // Consumed

                color = COLOR_CONSUMED.Multiply(ageRatio);
                height = GetHeight(data.GetConsumedRatio(maxProducedConsumed));
                lastHeight = lastData == null ? height : GetHeight(lastData.GetConsumedRatio(maxProducedConsumed));
                Draw(x, lastHeight, height, color);

                // Produced

                color = COLOR_PRODUCED.Multiply(ageRatio);
                height = GetHeight(data.GetProducedRatio(maxProducedConsumed));
                lastHeight = lastData == null ? height : GetHeight(lastData.GetProducedRatio(maxProducedConsumed));
                Draw(x, lastHeight, height, color);

                // Stored

                color = COLOR_STORED.Multiply(ageRatio);
                height = GetHeight(data.GetStoredRatio(maxStorage));
                lastHeight = lastData == null ? height : GetHeight(lastData.GetStoredRatio(maxStorage));
                Draw(x, lastHeight, height, color);

                x++;

                if (x >= LCD_PX)
                    x = 0;

                lastData = data;
            }

            var imgString = GetImageString(_colorArray, LCD_PX, LCD_PX);
            _lcd.WriteText(imgString);
        }

        void Draw(int x, int lastHeight, int height, Color3 color) {
            if (lastHeight > height)
                for (var h = height; h < lastHeight; h++)
                    _colorArray[h, x] = color.Packed;
            else if (lastHeight < height)
                for (var h = height; h > lastHeight; h--)
                    _colorArray[h, x] = color.Packed;
            else
                _colorArray[height, x] = color.Packed;
        }

        int GetHeight(float ratio) => (int)((1f - ratio) * (LCD_PX - 1));

        string GetImageString(int[,] colorArray, int width, int height) {
            var sb = new StringBuilder();
            for (var row = 0; row < height; row++) {
                for (var col = 0; col < width; col++) {
                    var thisColor = colorArray[row, col];
                    var colorChar = thisColor == 0 ? TRANS_FAKE_CHAR : ColorToChar((byte)(thisColor >> 16), (byte)(thisColor >> 8), (byte)thisColor);
                    sb.Append(colorChar);
                }
                if (row + 1 < height)
                    sb.Append("\n");
            }
            var imgString = sb.ToString()
                .Replace(trans178, spacer178)
                .Replace(trans8, spacer8)
                .Replace(trans4, spacer4)
                .Replace(trans2, spacer2)
                .Replace(trans, spacer);
            return imgString;
        }

        char ColorToChar(byte r, byte g, byte b) {
            return (char)(0xe100 + ((int)Math.Round(r / BIT_SPACING) << 6) + ((int)Math.Round(g / BIT_SPACING) << 3) + (int)Math.Round(b / BIT_SPACING));
        }

        Color3 GetClosestColor(Color3 pixelColor) {
            int R, G, B;
            R = (int)(Math.Round(pixelColor.R / BIT_SPACING) * BIT_SPACING);
            G = (int)(Math.Round(pixelColor.G / BIT_SPACING) * BIT_SPACING);
            B = (int)(Math.Round(pixelColor.B / BIT_SPACING) * BIT_SPACING);
            return new Color3(R, G, B);
        }

        public class PowerData {

            public float Stored;
            public float Produced;
            public float Consumed;
            public DateTime TimeStamp;

            private PowerData() { }

            public PowerData(float stored, float produced, float consumed) {
                Stored = stored;
                Produced = produced;
                Consumed = consumed;
                TimeStamp = DateTime.Now;
            }

            public float GetStoredRatio(float maxStorage) => Stored / maxStorage;

            public float GetProducedRatio(float maxProducedConsumed) => Produced / maxProducedConsumed;

            public float GetConsumedRatio(float maxProducedConsumed) => Consumed / maxProducedConsumed;

            public float GetAge() => (float)(DateTime.Now - TimeStamp).TotalSeconds;
        }

        public struct Color3 {

            public readonly int R;
            public readonly int G;
            public readonly int B;
            public readonly int Packed;

            public Color3(int R, int G, int B) {
                this.R = R;
                this.G = G;
                this.B = B;
                Packed = (255 << 24) | (ClampColor(R) << 16) | (ClampColor(G) << 8) | ClampColor(B);
            }

            public Color3 Multiply(float amount) => new Color3(ClampColor((int)(R * amount)), ClampColor((int)(G * amount)), ClampColor((int)(B * amount)));

            private static int ClampColor(int value) => Math.Max(0, Math.Min(255, value));
        }

        


    }
}