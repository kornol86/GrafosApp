/// <summary>
/// Grafo implementado con LISTA DE ADYACENCIA:
/// Dictionary(vértice -> lista de aristas salientes).
/// Soporta grafos dirigidos / no dirigidos y ponderados / no ponderados.
/// </summary>
public class Grafo
{
    private readonly Dictionary<string, List<Arista>> _adyacencia = new(StringComparer.OrdinalIgnoreCase);
    // Mapea cualquier variante de mayúsculas al nombre "canónico" guardado.
    private readonly Dictionary<string, string> _nombres = new(StringComparer.OrdinalIgnoreCase);

    public string Nombre { get; }
    public bool Dirigido { get; }
    public bool Ponderado { get; }
    public int NumVertices => _adyacencia.Count;
    public int NumAristas { get; private set; }
    public IEnumerable<string> Vertices => _adyacencia.Keys;

    public Grafo(string nombre, bool dirigido, bool ponderado)
    {
        Nombre = nombre;
        Dirigido = dirigido;
        Ponderado = ponderado;
    }

    /// <summary>Devuelve el nombre canónico del vértice, o null si no existe. O(1).</summary>
    public string? Resolver(string nombre) =>
        _nombres.TryGetValue(nombre.Trim(), out var canonico) ? canonico : null;

    public bool ExisteVertice(string nombre) => Resolver(nombre) != null;

    /// <summary>Agrega un vértice. O(1). Devuelve false si ya existía.</summary>
    public bool AgregarVertice(string nombre)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0) throw new ArgumentException("El nombre del vértice no puede estar vacío.");
        if (_adyacencia.ContainsKey(nombre)) return false;
        _adyacencia[nombre] = new List<Arista>();
        _nombres[nombre] = nombre;
        return true;
    }

    /// <summary>
    /// Agrega una arista (crea los vértices si no existen). O(grado) por la verificación de duplicados.
    /// Devuelve false si la arista ya existía.
    /// </summary>
    public bool AgregarArista(string origen, string destino, double peso = 1)
    {
        if (string.IsNullOrWhiteSpace(origen)) throw new ArgumentException("El origen no puede estar vacío.", nameof(origen));
        if (string.IsNullOrWhiteSpace(destino)) throw new ArgumentException("El destino no puede estar vacío.", nameof(destino));
        if (!double.IsFinite(peso) || peso < 0)
            throw new ArgumentException("El peso debe ser un número finito y no negativo.", nameof(peso));

        origen = origen.Trim();
        destino = destino.Trim();
        AgregarVertice(origen);
        AgregarVertice(destino);
        string o = _nombres[origen];
        string d = _nombres[destino];

        if (ExisteArista(o, d)) return false;

        _adyacencia[o].Add(new Arista(o, d, peso));
        if (!Dirigido && !Igual(o, d))
            _adyacencia[d].Add(new Arista(d, o, peso));
        NumAristas++;
        return true;
    }

    public bool ExisteArista(string origen, string destino) =>
        _adyacencia.TryGetValue(origen.Trim(), out var lista) && lista.Any(a => Igual(a.Destino, destino.Trim()));

    /// <summary>Elimina una arista. O(grado).</summary>
    public bool EliminarArista(string origen, string destino)
    {
        if (!_adyacencia.TryGetValue(origen.Trim(), out var lista)) return false;
        if (lista.RemoveAll(a => Igual(a.Destino, destino.Trim())) == 0) return false;
        if (!Dirigido && !Igual(origen.Trim(), destino.Trim()))
            _adyacencia[destino.Trim()].RemoveAll(a => Igual(a.Destino, origen.Trim()));
        NumAristas--;
        return true;
    }

    /// <summary>Elimina un vértice y todas sus aristas. O(V + E).</summary>
    public bool EliminarVertice(string nombre)
    {
        nombre = nombre.Trim();
        if (!_adyacencia.TryGetValue(nombre, out var salientes)) return false;

        int entrantes = 0;
        foreach (var (clave, lista) in _adyacencia)
        {
            if (Igual(clave, nombre)) continue;
            entrantes += lista.RemoveAll(a => Igual(a.Destino, nombre));
        }

        int quitadas = Dirigido
            ? salientes.Count + entrantes
            : entrantes + (salientes.Any(a => Igual(a.Destino, nombre)) ? 1 : 0);

        _adyacencia.Remove(nombre);
        _nombres.Remove(nombre);
        NumAristas -= quitadas;
        return true;
    }

    /// <summary>Aristas salientes del vértice. O(1).</summary>
    public IReadOnlyList<Arista> Vecinos(string vertice) => _adyacencia[vertice];

    /// <summary>Todas las aristas del grafo (en no dirigidos, cada una aparece una sola vez).</summary>
    public IEnumerable<Arista> TodasLasAristas()
    {
        foreach (var lista in _adyacencia.Values)
            foreach (var a in lista)
                if (Dirigido || string.Compare(a.Origen, a.Destino, StringComparison.OrdinalIgnoreCase) <= 0)
                    yield return a;
    }

    /// <summary>Grado de entrada de todos los vértices. O(V + E).</summary>
    public Dictionary<string, int> GradosEntrada()
    {
        var grados = _adyacencia.Keys.ToDictionary(v => v, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var lista in _adyacencia.Values)
            foreach (var a in lista)
                grados[a.Destino]++;
        return grados;
    }

    private static bool Igual(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
