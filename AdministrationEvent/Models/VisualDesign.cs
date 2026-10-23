using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace AdministrationEvent.Models
{
    public class CanvasDocument
    {
        public List<CanvasSection> Sections { get; set; } = new();
        public string Validate()
        {
            if (Sections == null || Sections.Count < 1 || Sections.Count > 30 || Sections.Any(s => s == null)) return "Usá entre 1 y 30 secciones.";
            if (Sections.Sum(s => s.Elements?.Count ?? 0) > 300) return "El diseño admite hasta 300 elementos.";
            foreach (var s in Sections) {
                if ((s.Name?.Length ?? 0) > 80 || s.Height < 200 || s.Height > 2400 || !Color(s.Background) || !Asset(s.BackgroundImage) || s.Elements == null || s.Elements.Any(e => e == null)) return "Revisá el fondo y el tamaño de las secciones.";
                foreach (var e in s.Elements) {
                    if (!new[] { "title", "text", "image", "button", "countdown" }.Contains(e.Type) || e.X < 0 || e.Y < 0 || e.Width < 3 || e.Width > 100 || e.Height < 20 || e.Height > s.Height || e.X + e.Width > 100.01 || e.Y + e.Height > s.Height + .01 || !double.IsFinite(e.X) || !double.IsFinite(e.Y) || !double.IsFinite(e.Width) || !double.IsFinite(e.Height) || !Color(e.Color) || !Color(e.Background) || (e.Text?.Length ?? 0) > 5000 || (e.FontSize < 10 || e.FontSize > 160) || !new[] { "left", "center", "right" }.Contains(e.Align) || !Regex.IsMatch(e.Font ?? "", "^[a-zA-Z0-9 ,'-]{1,120}$") || !FontAsset(e.FontUrl) || !Asset(e.Image) || !Link(e.Link)) return "Hay un elemento con texto, tamaño o enlace inválido.";
                }
            }
            return null;
        }
        static bool Color(string v) => v != null && Regex.IsMatch(v, "^#[a-fA-F0-9]{6}$");
        static bool Link(string v) => string.IsNullOrEmpty(v) || (v.Length <= 2000 && Uri.TryCreate(v, UriKind.Absolute, out var u) && u.Scheme == "https" && string.IsNullOrEmpty(u.UserInfo));
        static bool FontAsset(string v) => string.IsNullOrEmpty(v) || (v.Length <= 2000 && Regex.IsMatch(v, @"^/fonts/[a-zA-Z0-9 _/-]+\.(?:ttf|otf|woff|woff2)$") && !v.Contains("//") && !v.Contains(".."));
        static bool Asset(string v) => string.IsNullOrEmpty(v) || (v.Length <= 2000 && Regex.IsMatch(v, @"^/images/[a-zA-Z0-9_/-]+\.[a-zA-Z0-9]+$") && !v.Contains("//") && !v.Contains(".."));
    }
    public class CanvasSection
    {
        public string Name { get; set; } = "Sección";
        public int Height { get; set; } = 700;
        public string Background { get; set; } = "#fff8eb";
        public string BackgroundImage { get; set; } = "";
        public List<CanvasElement> Elements { get; set; } = new();
    }
    public class CanvasElement
    {
        public string Type { get; set; } = "text";
        public string Text { get; set; } = "Escribí tu texto";
        public double X { get; set; } = 10;
        public double Y { get; set; } = 50;
        public double Width { get; set; } = 80;
        public double Height { get; set; } = 100;
        public int FontSize { get; set; } = 28;
        public string Font { get; set; } = "Georgia, serif";
        public string FontUrl { get; set; } = "";
        public string Color { get; set; } = "#365d53";
        public string Background { get; set; } = "#fff8eb";
        public string Align { get; set; } = "center";
        public string Image { get; set; } = "";
        public string Link { get; set; } = "";
    }
}
