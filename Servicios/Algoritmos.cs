/// <summary>Algoritmos sobre grafos. V = vértices, E = aristas.</summary>
public static class Algoritmos
{
    /// <summary>Recorrido en anchura (BFS). O(V + E).</summary>
    public static List<(string Vertice, int Nivel)> BFS(Grafo g, string inicio)
    {
        var resultado = new List<(string, int)>();
        var visitados = new HashSet<string> { inicio };
        var cola = new Queue<(string Vertice, int Nivel)>();
        cola.Enqueue((inicio, 0));

        while (cola.Count > 0)
        {
            var (v, nivel) = cola.Dequeue();
            resultado.Add((v, nivel));
            foreach (var a in g.Vecinos(v))
                if (visitados.Add(a.Destino))
                    cola.Enqueue((a.Destino, nivel + 1));
        }
        return resultado;
    }

    /// <summary>Recorrido en profundidad (DFS), iterativo con pila. O(V + E).</summary>
    public static List<string> DFS(Grafo g, string inicio)
    {
        var orden = new List<string>();
        var visitados = new HashSet<string>();
        var pila = new Stack<string>();
        pila.Push(inicio);

        while (pila.Count > 0)
        {
            string v = pila.Pop();
            if (!visitados.Add(v)) continue;
            orden.Add(v);

            var vecinos = g.Vecinos(v);
            for (int i = vecinos.Count - 1; i >= 0; i--)   // inverso para respetar el orden de lectura
                if (!visitados.Contains(vecinos[i].Destino))
                    pila.Push(vecinos[i].Destino);
        }
        return orden;
    }

    /// <summary>Camino de menor costo (Dijkstra con cola de prioridad). O((V + E) log V).</summary>
    public static (List<string> Ruta, double Costo)? RutaMasCorta(Grafo g, string origen, string destino)
    {
        var dist = new Dictionary<string, double> { [origen] = 0 };
        var previo = new Dictionary<string, string>();
        var cerrados = new HashSet<string>();
        var cola = new PriorityQueue<string, double>();
        cola.Enqueue(origen, 0);

        while (cola.TryDequeue(out string? u, out double du))
        {
            if (!cerrados.Add(u)) continue;
            if (u == destino) break;

            foreach (var a in g.Vecinos(u))
            {
                double nuevo = du + a.Peso;
                if (!dist.TryGetValue(a.Destino, out double actual) || nuevo < actual)
                {
                    dist[a.Destino] = nuevo;
                    previo[a.Destino] = u;
                    cola.Enqueue(a.Destino, nuevo);
                }
            }
        }

        if (!dist.ContainsKey(destino)) return null;

        var ruta = new List<string>();
        for (string? v = destino; v != null; v = previo.GetValueOrDefault(v))
            ruta.Add(v);
        ruta.Reverse();
        return (ruta, dist[destino]);
    }

    /// <summary>Componentes conexas (ignora la dirección de las aristas). O(V + E).</summary>
    public static List<List<string>> ComponentesConexas(Grafo g)
    {
        var vecinos = g.Vertices.ToDictionary(v => v, _ => new List<string>());
        foreach (var a in g.TodasLasAristas())
        {
            vecinos[a.Origen].Add(a.Destino);
            if (g.Dirigido) vecinos[a.Destino].Add(a.Origen);   // en no dirigidos ya está la arista inversa
        }

        var visitados = new HashSet<string>();
        var componentes = new List<List<string>>();
        foreach (var inicio in g.Vertices)
        {
            if (visitados.Contains(inicio)) continue;
            var componente = new List<string>();
            var cola = new Queue<string>();
            visitados.Add(inicio);
            cola.Enqueue(inicio);
            while (cola.Count > 0)
            {
                string v = cola.Dequeue();
                componente.Add(v);
                foreach (var w in vecinos[v])
                    if (visitados.Add(w)) cola.Enqueue(w);
            }
            componentes.Add(componente);
        }
        return componentes;
    }

    /// <summary>
    /// Orden topológico por niveles (algoritmo de Kahn). O(V + E).
    /// Devuelve null si el grafo no es dirigido o contiene un ciclo.
    /// </summary>
    public static List<List<string>>? OrdenTopologico(Grafo g)
    {
        if (!g.Dirigido) return null;

        var gradoEntrada = g.GradosEntrada();
        var actual = g.Vertices.Where(v => gradoEntrada[v] == 0).ToList();
        var niveles = new List<List<string>>();
        int procesados = 0;

        while (actual.Count > 0)
        {
            niveles.Add(actual);
            procesados += actual.Count;
            var siguiente = new List<string>();
            foreach (var u in actual)
                foreach (var a in g.Vecinos(u))
                    if (--gradoEntrada[a.Destino] == 0)
                        siguiente.Add(a.Destino);
            actual = siguiente;
        }
        return procesados == g.NumVertices ? niveles : null;
    }
}
