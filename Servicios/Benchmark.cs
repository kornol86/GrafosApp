/// <summary>Medición de tiempos de ejecución con Stopwatch.</summary>
public static class Benchmark
{
    /// <summary>Ejecuta la acción N veces y devuelve el tiempo promedio en microsegundos.</summary>
    public static double MedirMicrosegundos(Action accion, int repeticiones)
    {
        accion();   // calentamiento (JIT)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < repeticiones; i++) accion();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds * 1000.0 / repeticiones;
    }

    /// <summary>Tiempos de los algoritmos sobre el grafo cargado actualmente.</summary>
    public static void AnalizarGrafo(Grafo g, int repeticiones = 2000)
    {
        if (g.NumVertices == 0) { Console.WriteLine("El grafo está vacío."); return; }
        string inicio = g.Vertices.First();
        string fin = g.Vertices.Last();

        Console.WriteLine($"\n=== TIEMPOS SOBRE '{g.Nombre}' (V={g.NumVertices}, E={g.NumAristas}) ===");
        Console.WriteLine($"Promedio de {repeticiones} ejecuciones por algoritmo:\n");
        Console.WriteLine($"{"Operación",-32}{"Tiempo (µs)",14}");
        Console.WriteLine(new string('-', 46));

        Fila("BFS", () => Algoritmos.BFS(g, inicio), repeticiones);
        Fila("DFS", () => Algoritmos.DFS(g, inicio), repeticiones);
        Fila("Dijkstra (primero -> último)", () => Algoritmos.RutaMasCorta(g, inicio, fin), repeticiones);
        Fila("Componentes conexas", () => Algoritmos.ComponentesConexas(g), repeticiones);
        if (g.Dirigido)
            Fila("Orden topológico", () => Algoritmos.OrdenTopologico(g), repeticiones);
    }

    /// <summary>Prueba de escalabilidad con grafos aleatorios cada vez más grandes.</summary>
    public static void AnalisisEscalabilidad()
    {
        int[] tamanos = { 1_000, 5_000, 10_000, 50_000 };
        Console.WriteLine("\n=== ESCALABILIDAD (grafos aleatorios, E = 5·V, no dirigidos, ponderados) ===");
        Console.WriteLine($"{"V",8}{"E",9}{"Construir",12}{"BFS",10}{"DFS",10}{"Dijkstra",11}{"Compon.",10}   (ms)");
        Console.WriteLine(new string('-', 70));

        foreach (int v in tamanos)
        {
            Grafo? g = null;
            double construir = MedirMs(() => g = GenerarAleatorio(v, v * 5), 1);
            string inicio = "V0", fin = $"V{v - 1}";

            double bfs = MedirMs(() => Algoritmos.BFS(g!, inicio), 5);
            double dfs = MedirMs(() => Algoritmos.DFS(g!, inicio), 5);
            double dij = MedirMs(() => Algoritmos.RutaMasCorta(g!, inicio, fin), 5);
            double cc = MedirMs(() => Algoritmos.ComponentesConexas(g!), 5);

            Console.WriteLine($"{g!.NumVertices,8}{g.NumAristas,9}{construir,12:F1}{bfs,10:F2}{dfs,10:F2}{dij,11:F2}{cc,10:F2}");
        }
        Console.WriteLine("\nSi V y E crecen x5, los tiempos deben crecer ~x5 (BFS/DFS/Componentes son O(V+E))");
        Console.WriteLine("y un poco más en Dijkstra por el factor log V.");
    }

    /// <summary>Grafo conexo aleatorio (una cadena base + aristas al azar). Semilla fija = resultados repetibles.</summary>
    public static Grafo GenerarAleatorio(int vertices, int aristas, int semilla = 42)
    {
        var rnd = new Random(semilla);
        var g = new Grafo($"Aleatorio_{vertices}", dirigido: false, ponderado: true);
        for (int i = 0; i < vertices; i++) g.AgregarVertice($"V{i}");
        for (int i = 0; i < vertices - 1; i++) g.AgregarArista($"V{i}", $"V{i + 1}", rnd.Next(1, 101));

        int intentos = 0;
        while (g.NumAristas < aristas && intentos++ < aristas * 10)
            g.AgregarArista($"V{rnd.Next(vertices)}", $"V{rnd.Next(vertices)}", rnd.Next(1, 101));
        return g;
    }

    private static void Fila(string nombre, Action accion, int repeticiones) =>
        Console.WriteLine($"{nombre,-32}{MedirMicrosegundos(accion, repeticiones),14:F2}");

    private static double MedirMs(Action accion, int repeticiones)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < repeticiones; i++) accion();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds / repeticiones;
    }
}
