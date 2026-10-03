using System.Globalization;
using System.Security;
using System.Text;


/// <summary>
/// Exporta el grafo a una imagen vectorial SVG (sin librerías externas).
/// El archivo .svg se abre con cualquier navegador y se puede insertar en Word o PowerPoint.
///
/// Distribución de los vértices:
///  - Grafo dirigido sin ciclos: por capas (orden topológico), de arriba hacia abajo. Las aristas que saltan
///    varias capas pasan por "vértices virtuales" para no cruzar por detrás de otros vértices.
///  - Cualquier otro grafo: algoritmo de fuerzas (Fruchterman-Reingold): los vértices se repelen entre sí y las
///    aristas los atraen, por lo que los vértices conectados quedan cerca. Parte de una disposición circular
///    fija, así que el resultado es siempre el mismo para el mismo grafo.
/// </summary>
public static class ExportadorImagen
{
    private const int Ancho = 760, Alto = 680;
    private const int MargenSup = 90, MargenInf = 40;
    private const int MaxVertices = 40;

    private const string ColorNodo = "#E2EFD9", BordeNodo = "#548235";
    private const string ColorArista = "#7F7F7F", ColorRuta = "#C00000", ColorNodoRuta = "#FFD966";

    // Puntos intermedios (vértices virtuales) por los que pasa una arista larga.
    private static readonly IReadOnlyDictionary<(string, string), List<(double X, double Y)>> SinIntermedios =
        new Dictionary<(string, string), List<(double X, double Y)>>();

    /// <summary>
    /// Genera el archivo SVG. Si se entrega <paramref name="rutaResaltada"/> (lista de vértices),
    /// esa ruta se dibuja en rojo. Complejidad: O(V + E) para dibujar (más O(iteraciones · V²) si usa fuerzas).
    /// </summary>
    public static void ExportarSvg(Grafo g, string rutaArchivo, IReadOnlyList<string>? rutaResaltada = null)
    {
        if (g.NumVertices == 0)
            throw new InvalidOperationException("El grafo está vacío, no hay nada que dibujar.");
        if (g.NumVertices > MaxVertices)
            throw new InvalidOperationException($"El grafo tiene {g.NumVertices} vértices; el dibujo admite hasta {MaxVertices}.");

        var tam = g.Vertices.ToDictionary(v => v, v =>
        {
            var lineas = PartirNombre(v);
            double w = Math.Max(60.0, lineas.Max(l => l.Length) * 7.2 + 18);
            double h = lineas.Length == 1 ? 30.0 : 44.0;
            return (W: w, H: h, Lineas: lineas);
        });
        var (pos, intermedios) = CalcularPosiciones(g);

        var vertRuta = new HashSet<string>();
        var aristasRuta = new HashSet<(string, string)>();
        if (rutaResaltada != null)
        {
            for (int i = 0; i < rutaResaltada.Count; i++)
            {
                vertRuta.Add(rutaResaltada[i]);
                if (i == 0) continue;
                aristasRuta.Add((rutaResaltada[i - 1], rutaResaltada[i]));
                if (!g.Dirigido) aristasRuta.Add((rutaResaltada[i], rutaResaltada[i - 1]));
            }
        }

        var sb = new StringBuilder();
        var etiquetas = new StringBuilder();

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Ancho}\" height=\"{Alto}\" viewBox=\"0 0 {Ancho} {Alto}\" font-family=\"Segoe UI, Arial, sans-serif\">");
        sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
        sb.AppendLine("<defs>");
        sb.AppendLine(Flecha("flecha", ColorArista));
        sb.AppendLine(Flecha("flechaRuta", ColorRuta));
        sb.AppendLine("</defs>");

        string tipo = $"{(g.Dirigido ? "Dirigido" : "No dirigido")} · {(g.Ponderado ? "ponderado" : "no ponderado")} · {g.NumVertices} vértices · {g.NumAristas} aristas";
        sb.AppendLine($"<text x=\"20\" y=\"32\" font-size=\"20\" font-weight=\"bold\" fill=\"#222222\">{Esc(g.Nombre)}</text>");
        sb.AppendLine($"<text x=\"20\" y=\"54\" font-size=\"13\" fill=\"#666666\">{Esc(tipo)}</text>");
        if (rutaResaltada != null && rutaResaltada.Count > 0)
            sb.AppendLine($"<text x=\"20\" y=\"74\" font-size=\"13\" fill=\"{ColorRuta}\">Ruta resaltada: {Esc(string.Join(" → ", rutaResaltada))}</text>");

        // 1) Aristas (debajo de los vértices)
        foreach (var a in g.TodasLasAristas())
        {
            bool enRuta = aristasRuta.Contains((a.Origen, a.Destino));
            string color = enRuta ? ColorRuta : ColorArista;
            string grosor = enRuta ? "3.5" : "1.6";
            string marcador = g.Dirigido ? $" marker-end=\"url(#{(enRuta ? "flechaRuta" : "flecha")})\"" : "";
            var (ox, oy) = pos[a.Origen];
            var (tx, ty) = pos[a.Destino];

            if (a.Origen == a.Destino)   // lazo
            {
                double arriba = oy - tam[a.Origen].H / 2;
                sb.AppendLine($"<path d=\"M {F(ox - 12)} {F(arriba)} C {F(ox - 28)} {F(arriba - 36)}, {F(ox + 28)} {F(arriba - 36)}, {F(ox + 12)} {F(arriba)}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{grosor}\"{marcador}/>");
                continue;
            }

            // Recorrido de la arista: origen, (vértices virtuales), destino.
            var puntos = new List<(double X, double Y)> { (ox, oy) };
            if (intermedios.TryGetValue((a.Origen, a.Destino), out var medios)) puntos.AddRange(medios);
            puntos.Add((tx, ty));

            // El primer y el último tramo se recortan en el borde de las cajas.
            var siguiente = puntos[1];
            var anterior = puntos[puntos.Count - 2];
            var ini = Borde(ox, oy, tam[a.Origen].W / 2, tam[a.Origen].H / 2, siguiente.X - ox, siguiente.Y - oy);
            var fin = Borde(tx, ty, tam[a.Destino].W / 2, tam[a.Destino].H / 2, anterior.X - tx, anterior.Y - ty);

            // Si existe la arista contraria (A->B y B->A), se separan un poco para que no se superpongan.
            if (puntos.Count == 2 && g.Dirigido && g.ExisteArista(a.Destino, a.Origen))
            {
                double vx = tx - ox, vy = ty - oy;
                double largo = Math.Sqrt(vx * vx + vy * vy);
                double px = -vy / largo * 6, py = vx / largo * 6;
                ini = (ini.X + px, ini.Y + py);
                fin = (fin.X + px, fin.Y + py);
            }

            var trazo = new List<(double X, double Y)> { ini };
            for (int k = 1; k < puntos.Count - 1; k++) trazo.Add(puntos[k]);
            trazo.Add(fin);

            string coordenadas = string.Join(" ", trazo.Select(q => $"{F(q.X)},{F(q.Y)}"));
            sb.AppendLine($"<polyline points=\"{coordenadas}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{grosor}\" stroke-linejoin=\"round\"{marcador}/>");

            if (g.Ponderado)
            {
                int m = trazo.Count / 2;   // tramo central
                double mx = (trazo[m - 1].X + trazo[m].X) / 2, my = (trazo[m - 1].Y + trazo[m].Y) / 2;
                etiquetas.AppendLine($"<text x=\"{F(mx)}\" y=\"{F(my + 4)}\" font-size=\"12\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"{(enRuta ? ColorRuta : "#1F3864")}\" stroke=\"white\" stroke-width=\"4\" paint-order=\"stroke\">{F(a.Peso)}</text>");
            }
        }
        sb.Append(etiquetas);

        // 2) Vértices
        foreach (var v in g.Vertices)
        {
            var (cx, cy) = pos[v];
            var (w, h, lineas) = tam[v];
            bool resaltado = vertRuta.Contains(v);
            sb.AppendLine($"<rect x=\"{F(cx - w / 2)}\" y=\"{F(cy - h / 2)}\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"8\" fill=\"{(resaltado ? ColorNodoRuta : ColorNodo)}\" stroke=\"{(resaltado ? ColorRuta : BordeNodo)}\" stroke-width=\"{(resaltado ? "2.5" : "1.5")}\"/>");

            if (lineas.Length == 1)
                sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy + 4.5)}\" font-size=\"13\" text-anchor=\"middle\" fill=\"#1B1B1B\">{Esc(lineas[0])}</text>");
            else
                sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy - 3)}\" font-size=\"13\" text-anchor=\"middle\" fill=\"#1B1B1B\">{Esc(lineas[0])}<tspan x=\"{F(cx)}\" dy=\"15\">{Esc(lineas[1])}</tspan></text>");
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(rutaArchivo, sb.ToString(), new UTF8Encoding(false));
    }

    // ---------- Distribución de los vértices ----------

    private static (Dictionary<string, (double X, double Y)> Pos, IReadOnlyDictionary<(string, string), List<(double X, double Y)>> Intermedios)
        CalcularPosiciones(Grafo g)
    {
        var niveles = Algoritmos.OrdenTopologico(g);   // null si no es dirigido o tiene ciclos
        if (niveles != null && niveles.Count > 1) return PorCapas(g, niveles);
        return (PorFuerzas(g), SinIntermedios);
    }

    private static (Dictionary<string, (double X, double Y)> Pos, IReadOnlyDictionary<(string, string), List<(double X, double Y)>> Intermedios)
        PorCapas(Grafo g, List<List<string>> niveles)
    {
        var nivelDe = new Dictionary<string, int>();
        for (int i = 0; i < niveles.Count; i++)
            foreach (var v in niveles[i]) nivelDe[v] = i;

        // Capas que incluyen "vértices virtuales": una arista que salta varias capas deja uno en cada capa intermedia.
        var capas = niveles.Select(n => new List<string>(n)).ToList();
        var predecesores = new Dictionary<string, List<string>>();
        foreach (var v in g.Vertices) predecesores[v] = new List<string>();
        var cadenas = new Dictionary<(string, string), List<string>>();

        foreach (var a in g.TodasLasAristas())
        {
            int salto = nivelDe[a.Destino] - nivelDe[a.Origen];
            string anterior = a.Origen;
            var virtuales = new List<string>();
            for (int k = 1; k < salto; k++)
            {
                string virtual_ = $"#{a.Origen}>{a.Destino}#{k}";
                capas[nivelDe[a.Origen] + k].Add(virtual_);
                predecesores[virtual_] = new List<string> { anterior };
                virtuales.Add(virtual_);
                anterior = virtual_;
            }
            predecesores[a.Destino].Add(anterior);
            if (virtuales.Count > 0) cadenas[(a.Origen, a.Destino)] = virtuales;
        }

        var posTodos = new Dictionary<string, (double X, double Y)>();
        var xs = new Dictionary<string, double>();
        double yArriba = MargenSup + 25, yAbajo = Alto - MargenInf - 25;

        for (int i = 0; i < capas.Count; i++)
        {
            var capa = capas[i];
            // Se ordena cada capa por la posición media de sus predecesores (menos cruces de aristas).
            if (i > 0) capa = capa.OrderBy(v => predecesores[v].Average(p => xs[p])).ToList();

            double y = yArriba + i * (yAbajo - yArriba) / (capas.Count - 1);
            for (int j = 0; j < capa.Count; j++)
            {
                double x = (j + 0.5) * Ancho / capa.Count;
                xs[capa[j]] = x;
                posTodos[capa[j]] = (x, y);
            }
        }

        var pos = new Dictionary<string, (double X, double Y)>();
        foreach (var v in g.Vertices) pos[v] = posTodos[v];

        var intermedios = new Dictionary<(string, string), List<(double X, double Y)>>();
        foreach (var (arista, virtuales) in cadenas)
            intermedios[arista] = virtuales.Select(nombre => posTodos[nombre]).ToList();

        return (pos, intermedios);
    }

    private static Dictionary<string, (double X, double Y)> Circular(Grafo g)
    {
        var orden = new List<string>();
        var vistos = new HashSet<string>();
        foreach (var v in g.Vertices)
            if (!vistos.Contains(v))
                foreach (var w in Algoritmos.DFS(g, v))
                    if (vistos.Add(w)) orden.Add(w);

        double cx = Ancho / 2.0, cy = (MargenSup + Alto - MargenInf) / 2.0;
        double rx = Ancho / 2.0 - 80, ry = (Alto - MargenSup - MargenInf) / 2.0 - 30;

        var pos = new Dictionary<string, (double X, double Y)>();
        for (int i = 0; i < orden.Count; i++)
        {
            double ang = 2 * Math.PI * i / orden.Count - Math.PI / 2;
            pos[orden[i]] = (cx + rx * Math.Cos(ang), cy + ry * Math.Sin(ang));
        }
        return pos;
    }

    /// <summary>Fruchterman-Reingold determinista. O(iteraciones · (V² + E)); V está limitado a 40.</summary>
    private static Dictionary<string, (double X, double Y)> PorFuerzas(Grafo g)
    {
        var inicial = Circular(g);
        var nombres = g.Vertices.ToList();
        int n = nombres.Count;
        var indice = new Dictionary<string, int>();
        for (int i = 0; i < n; i++) indice[nombres[i]] = i;

        var x = new double[n];
        var y = new double[n];
        for (int i = 0; i < n; i++) (x[i], y[i]) = inicial[nombres[i]];

        var aristas = g.TodasLasAristas()
            .Where(a => a.Origen != a.Destino)
            .Select(a => (I: indice[a.Origen], J: indice[a.Destino]))
            .ToList();

        double anchoUtil = Ancho - 140, altoUtil = Alto - MargenSup - MargenInf - 70;
        double k = 0.9 * Math.Sqrt(anchoUtil * altoUtil / n);
        double temperatura = anchoUtil / 8;
        const int Iteraciones = 300;

        for (int it = 0; it < Iteraciones; it++)
        {
            var fx = new double[n];
            var fy = new double[n];

            for (int i = 0; i < n; i++)            // repulsión entre todos los pares
                for (int j = i + 1; j < n; j++)
                {
                    double ddx = x[i] - x[j], ddy = y[i] - y[j];
                    double d = Math.Max(Math.Sqrt(ddx * ddx + ddy * ddy), 0.01);
                    double f = k * k / d;
                    fx[i] += ddx / d * f; fy[i] += ddy / d * f;
                    fx[j] -= ddx / d * f; fy[j] -= ddy / d * f;
                }

            foreach (var (i, j) in aristas)        // atracción entre vértices conectados
            {
                double ddx = x[i] - x[j], ddy = y[i] - y[j];
                double d = Math.Max(Math.Sqrt(ddx * ddx + ddy * ddy), 0.01);
                double f = d * d / k;
                fx[i] -= ddx / d * f; fy[i] -= ddy / d * f;
                fx[j] += ddx / d * f; fy[j] += ddy / d * f;
            }

            for (int i = 0; i < n; i++)            // desplazamiento limitado por la "temperatura"
            {
                double d = Math.Max(Math.Sqrt(fx[i] * fx[i] + fy[i] * fy[i]), 0.01);
                double paso = Math.Min(d, temperatura);
                x[i] += fx[i] / d * paso;
                y[i] += fy[i] / d * paso;
            }
            temperatura = anchoUtil / 8 * (1 - (it + 1) / (double)Iteraciones);
        }

        // Ajusta el resultado al área de dibujo.
        double minX = x.Min(), maxX = x.Max(), minY = y.Min(), maxY = y.Max();
        double x0 = 75, x1 = Ancho - 75, y0 = MargenSup + 35, y1 = Alto - MargenInf - 35;
        var pos = new Dictionary<string, (double X, double Y)>();
        for (int i = 0; i < n; i++)
        {
            double px = maxX - minX < 1e-6 ? (x0 + x1) / 2 : x0 + (x[i] - minX) / (maxX - minX) * (x1 - x0);
            double py = maxY - minY < 1e-6 ? (y0 + y1) / 2 : y0 + (y[i] - minY) / (maxY - minY) * (y1 - y0);
            pos[nombres[i]] = (px, py);
        }
        return pos;
    }

    // ---------- Utilidades de dibujo ----------

    /// <summary>Punto donde la recta que sale del centro del rectángulo en la dirección (vx, vy) cruza su borde.</summary>
    private static (double X, double Y) Borde(double cx, double cy, double mediaW, double mediaH, double vx, double vy)
    {
        double sx = Math.Abs(vx) < 1e-9 ? double.MaxValue : mediaW / Math.Abs(vx);
        double sy = Math.Abs(vy) < 1e-9 ? double.MaxValue : mediaH / Math.Abs(vy);
        double s = Math.Min(sx, sy);
        return (cx + vx * s, cy + vy * s);
    }

    /// <summary>Divide en dos líneas los nombres largos con espacios (para cajas más angostas).</summary>
    private static string[] PartirNombre(string nombre)
    {
        if (nombre.Length <= 15 || !nombre.Contains(' ')) return new[] { nombre };

        int mitad = nombre.Length / 2, corte = -1;
        for (int i = 0; i < nombre.Length; i++)
            if (nombre[i] == ' ' && (corte == -1 || Math.Abs(i - mitad) < Math.Abs(corte - mitad)))
                corte = i;
        return new[] { nombre.Substring(0, corte), nombre.Substring(corte + 1) };
    }

    private static string Flecha(string id, string color) =>
        $"<marker id=\"{id}\" viewBox=\"0 0 10 10\" refX=\"10\" refY=\"5\" markerUnits=\"userSpaceOnUse\" markerWidth=\"13\" markerHeight=\"13\" orient=\"auto\"><path d=\"M 0 0 L 10 5 L 0 10 z\" fill=\"{color}\"/></marker>";

    private static string Esc(string texto) => SecurityElement.Escape(texto) ?? "";

    private static string F(double numero) => numero.ToString("0.##", CultureInfo.InvariantCulture);
}
