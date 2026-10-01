/// <summary>
/// Lee un grafo desde un bloc de notas (.txt) con este formato:
///
///   # comentario
///   NOMBRE=Mi grafo
///   DIRIGIDO=SI|NO
///   PONDERADO=SI|NO
///   VERTICES
///   A
///   B
///   ARISTAS
///   A;B;10
/// </summary>
public static class LectorArchivo
{
    public static Grafo Cargar(string ruta)
    {
        if (!File.Exists(ruta)) throw new FileNotFoundException($"No se encontró el archivo: {ruta}");

        string nombre = Path.GetFileNameWithoutExtension(ruta);
        bool dirigido = false, ponderado = false;
        var vertices = new List<string>();
        var aristas = new List<(string Origen, string Destino, double Peso)>();
        string seccion = "";
        int numLinea = 0;

        foreach (var cruda in File.ReadAllLines(ruta))
        {
            numLinea++;
            string linea = cruda.Trim();
            if (linea.Length == 0 || linea.StartsWith('#')) continue;

            if (linea.Equals("VERTICES", StringComparison.OrdinalIgnoreCase)) { seccion = "V"; continue; }
            if (linea.Equals("ARISTAS", StringComparison.OrdinalIgnoreCase)) { seccion = "A"; continue; }

            if (seccion == "")
            {
                var par = linea.Split('=', 2);
                if (par.Length != 2) throw new FormatException($"Línea {numLinea}: se esperaba CLAVE=VALOR.");
                string clave = par[0].Trim().ToUpperInvariant(), valor = par[1].Trim();
                switch (clave)
                {
                    case "NOMBRE": nombre = valor; break;
                    case "DIRIGIDO": dirigido = valor.Equals("SI", StringComparison.OrdinalIgnoreCase); break;
                    case "PONDERADO": ponderado = valor.Equals("SI", StringComparison.OrdinalIgnoreCase); break;
                    default: throw new FormatException($"Línea {numLinea}: clave desconocida '{clave}'.");
                }
            }
            else if (seccion == "V")
            {
                vertices.Add(linea);
            }
            else
            {
                var partes = linea.Split(';');
                if (partes.Length < 2)
                    throw new FormatException($"Línea {numLinea}: formato de arista inválido (origen;destino;peso).");

                double peso = 1;
                if (ponderado && partes.Length >= 3 &&
                    !double.TryParse(partes[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out peso))
                    throw new FormatException($"Línea {numLinea}: peso inválido '{partes[2]}'.");

                aristas.Add((partes[0].Trim(), partes[1].Trim(), peso));
            }
        }

        var grafo = new Grafo(nombre, dirigido, ponderado);
        foreach (var v in vertices) grafo.AgregarVertice(v);
        foreach (var (o, d, p) in aristas) grafo.AgregarArista(o, d, p);
        return grafo;
    }
}
