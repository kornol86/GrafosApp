internal static class Program
{
    private static Grafo? _grafo;
    private static readonly string CarpetaDatos = Path.Combine(AppContext.BaseDirectory, "Datos");

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool salir = false;

        while (!salir)
        {
            MostrarMenu();
            string? entrada = Console.ReadLine();
            if (entrada == null) return;
            string opcion = entrada.Trim();
            try { salir = Ejecutar(opcion); }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }

            if (!salir)
            {
                Console.WriteLine("\nPresione Enter para continuar...");
                Console.ReadLine();
            }
        }
    }

    private static void MostrarMenu()
    {
        if (!Console.IsOutputRedirected) Console.Clear();
        Console.WriteLine("==========================================");
        Console.WriteLine("        GRAFOS - MENÚ PRINCIPAL");
        Console.WriteLine("==========================================");
        Console.WriteLine(_grafo == null
            ? "Grafo cargado: (ninguno)"
            : $"Grafo cargado: {_grafo.Nombre} [V={_grafo.NumVertices}, E={_grafo.NumAristas}]");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine(" 1. Cargar grafo desde archivo .txt");
        Console.WriteLine(" 2. Reporte: resumen y grados");
        Console.WriteLine(" 3. Reporte: lista de adyacencia");
        Console.WriteLine(" 4. Reporte: matriz de adyacencia");
        Console.WriteLine(" 5. Reporte: lista de aristas");
        Console.WriteLine(" 6. Recorrido en anchura (BFS)");
        Console.WriteLine(" 7. Recorrido en profundidad (DFS)");
        Console.WriteLine(" 8. Ruta más corta (Dijkstra)");
        Console.WriteLine(" 9. Componentes conexas");
        Console.WriteLine("10. Orden topológico (grafos dirigidos)");
        Console.WriteLine("11. Agregar vértice / arista");
        Console.WriteLine("12. Eliminar vértice / arista");
        Console.WriteLine("13. Análisis de tiempos de ejecución");
        Console.WriteLine(" 0. Salir");
        Console.Write("\nOpción: ");
    }

    private static bool Ejecutar(string opcion)
    {
        if (opcion == "0") return true;
        if (opcion == "1") { CargarArchivo(); return false; }

        if (!int.TryParse(opcion, out int n) || n < 2 || n > 13)
        {
            Console.WriteLine("Opción inválida.");
            return false;
        }
        if (_grafo == null)
        {
            Console.WriteLine("Primero cargue un grafo (opción 1).");
            return false;
        }

        switch (n)
        {
            case 2: Reportes.Resumen(_grafo); break;
            case 3: Reportes.ListaAdyacencia(_grafo); break;
            case 4: Reportes.MatrizAdyacencia(_grafo); break;
            case 5: Reportes.ListaAristas(_grafo); break;
            case 6: RecorridoBFS(); break;
            case 7: RecorridoDFS(); break;
            case 8: RutaMasCorta(); break;
            case 9: Componentes(); break;
            case 10: OrdenTopologico(); break;
            case 11: Agregar(); break;
            case 12: Eliminar(); break;
            case 13: AnalisisTiempos(); break;
        }
        return false;
    }

    // ---------- Opciones del menú ----------

    private static void CargarArchivo()
    {
        var archivos = Directory.Exists(CarpetaDatos)
            ? Directory.GetFiles(CarpetaDatos, "*.txt").OrderBy(f => f).ToArray()
            : Array.Empty<string>();

        Console.WriteLine("\nArchivos disponibles en la carpeta Datos:");
        for (int i = 0; i < archivos.Length; i++)
            Console.WriteLine($"  {i + 1}. {Path.GetFileName(archivos[i])}");

        Console.Write("Elija un número o escriba la ruta completa de un .txt: ");
        string entrada = Console.ReadLine()?.Trim() ?? "";
        string ruta = int.TryParse(entrada, out int idx) && idx >= 1 && idx <= archivos.Length
            ? archivos[idx - 1]
            : entrada.Trim('"');

        _grafo = LectorArchivo.Cargar(ruta);
        Console.WriteLine($"\nGrafo '{_grafo.Nombre}' cargado: {_grafo.NumVertices} vértices, {_grafo.NumAristas} aristas.");
    }

    private static void RecorridoBFS()
    {
        string? inicio = PedirVertice("Vértice de inicio: ");
        if (inicio == null) return;

        Console.WriteLine($"\nBFS desde {inicio} (por niveles de distancia en saltos):");
        foreach (var grupo in Algoritmos.BFS(_grafo!, inicio).GroupBy(x => x.Nivel))
            Console.WriteLine($"  Nivel {grupo.Key}: {string.Join(", ", grupo.Select(x => x.Vertice))}");
    }

    private static void RecorridoDFS()
    {
        string? inicio = PedirVertice("Vértice de inicio: ");
        if (inicio == null) return;

        Console.WriteLine($"\nDFS desde {inicio}:");
        Console.WriteLine("  " + string.Join(" -> ", Algoritmos.DFS(_grafo!, inicio)));
    }

    private static void RutaMasCorta()
    {
        string? origen = PedirVertice("Vértice origen: ");
        if (origen == null) return;
        string? destino = PedirVertice("Vértice destino: ");
        if (destino == null) return;

        var resultado = Algoritmos.RutaMasCorta(_grafo!, origen, destino);
        if (resultado == null)
        {
            Console.WriteLine($"\nNo existe ruta de {origen} a {destino}.");
            return;
        }

        var (ruta, costo) = resultado.Value;
        Console.WriteLine($"\nRuta: {string.Join(" -> ", ruta)}");
        Console.WriteLine(_grafo!.Ponderado
            ? $"Costo total: {costo:0.##}"
            : $"Saltos: {costo:0}");
    }

    private static void Componentes()
    {
        var componentes = Algoritmos.ComponentesConexas(_grafo!);
        Console.WriteLine($"\nComponentes conexas: {componentes.Count}");
        for (int i = 0; i < componentes.Count; i++)
            Console.WriteLine($"  {i + 1}. ({componentes[i].Count} vértices) {string.Join(", ", componentes[i])}");
    }

    private static void OrdenTopologico()
    {
        if (!_grafo!.Dirigido)
        {
            Console.WriteLine("El orden topológico solo aplica a grafos dirigidos.");
            return;
        }

        var niveles = Algoritmos.OrdenTopologico(_grafo);
        if (niveles == null)
        {
            Console.WriteLine("El grafo contiene un ciclo: no existe orden topológico.");
            return;
        }

        Console.WriteLine("\nOrden topológico por niveles (cada nivel solo depende de niveles anteriores):");
        for (int i = 0; i < niveles.Count; i++)
            Console.WriteLine($"  Nivel {i + 1}: {string.Join(", ", niveles[i])}");
    }

    private static void Agregar()
    {
        Console.Write("¿Agregar (1) vértice o (2) arista?: ");
        string tipo = Console.ReadLine()?.Trim() ?? "";

        if (tipo == "1")
        {
            Console.Write("Nombre del vértice: ");
            bool nuevo = _grafo!.AgregarVertice(Console.ReadLine() ?? "");
            Console.WriteLine(nuevo ? "Vértice agregado." : "Ese vértice ya existe.");
        }
        else if (tipo == "2")
        {
            Console.Write("Origen (se crea si no existe): ");
            string origen = Console.ReadLine() ?? "";
            Console.Write("Destino (se crea si no existe): ");
            string destino = Console.ReadLine() ?? "";

            double peso = 1;
            if (_grafo!.Ponderado)
            {
                Console.Write("Peso: ");
                string texto = (Console.ReadLine() ?? "").Replace(',', '.');
                if (!double.TryParse(texto, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out peso))
                {
                    Console.WriteLine("Peso inválido.");
                    return;
                }
            }
            Console.WriteLine(_grafo.AgregarArista(origen, destino, peso) ? "Arista agregada." : "Esa arista ya existe.");
        }
        else Console.WriteLine("Opción inválida.");
    }

    private static void Eliminar()
    {
        Console.Write("¿Eliminar (1) vértice o (2) arista?: ");
        string tipo = Console.ReadLine()?.Trim() ?? "";

        if (tipo == "1")
        {
            string? v = PedirVertice("Vértice a eliminar: ");
            if (v != null) Console.WriteLine(_grafo!.EliminarVertice(v) ? "Vértice eliminado." : "No se pudo eliminar.");
        }
        else if (tipo == "2")
        {
            string? o = PedirVertice("Origen: ");
            if (o == null) return;
            string? d = PedirVertice("Destino: ");
            if (d == null) return;
            Console.WriteLine(_grafo!.EliminarArista(o, d) ? "Arista eliminada." : "Esa arista no existe.");
        }
        else Console.WriteLine("Opción inválida.");
    }

    private static void AnalisisTiempos()
    {
        Benchmark.AnalizarGrafo(_grafo!);
        Benchmark.AnalisisEscalabilidad();

        Console.WriteLine("\n=== COMPLEJIDAD TEÓRICA (lista de adyacencia) ===");
        Console.WriteLine($"{"Operación",-28}{"Complejidad",-18}");
        Console.WriteLine(new string('-', 46));
        Console.WriteLine($"{"Agregar vértice",-28}{"O(1)",-18}");
        Console.WriteLine($"{"Agregar arista",-28}{"O(grado)",-18}");
        Console.WriteLine($"{"Consultar vecinos",-28}{"O(1) + O(grado)",-18}");
        Console.WriteLine($"{"Eliminar arista",-28}{"O(grado)",-18}");
        Console.WriteLine($"{"Eliminar vértice",-28}{"O(V + E)",-18}");
        Console.WriteLine($"{"BFS / DFS",-28}{"O(V + E)",-18}");
        Console.WriteLine($"{"Componentes conexas",-28}{"O(V + E)",-18}");
        Console.WriteLine($"{"Orden topológico (Kahn)",-28}{"O(V + E)",-18}");
        Console.WriteLine($"{"Dijkstra (cola prioridad)",-28}{"O((V + E) log V)",-18}");
        Console.WriteLine($"{"Espacio",-28}{"O(V + E)",-18}");
    }

    // ---------- Utilidades ----------

    /// <summary>Pide un vértice y lo valida contra el grafo (sin distinguir mayúsculas).</summary>
    private static string? PedirVertice(string mensaje)
    {
        Console.Write(mensaje);
        string entrada = Console.ReadLine() ?? "";
        string? vertice = _grafo!.Resolver(entrada);
        if (vertice == null) Console.WriteLine($"El vértice '{entrada.Trim()}' no existe.");
        return vertice;
    }
}
