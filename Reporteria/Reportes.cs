/// <summary>Reportes para visualizar y consultar los elementos del grafo.</summary>
public static class Reportes
{
    public static void Resumen(Grafo g)
    {
        Console.WriteLine($"\n=== RESUMEN: {g.Nombre} ===");
        Console.WriteLine($"Tipo:      {(g.Dirigido ? "Dirigido" : "No dirigido")}, {(g.Ponderado ? "ponderado" : "no ponderado")}");
        Console.WriteLine($"Vértices:  {g.NumVertices}");
        Console.WriteLine($"Aristas:   {g.NumAristas}");

        double n = g.NumVertices;
        double maximo = g.Dirigido ? n * (n - 1) : n * (n - 1) / 2.0;
        Console.WriteLine($"Densidad:  {(maximo > 0 ? g.NumAristas / maximo : 0):P1}");
        TablaGrados(g);
    }

    public static void TablaGrados(Grafo g)
    {
        Console.WriteLine("\n--- Grados de los vértices ---");
        if (g.Dirigido)
        {
            var entrada = g.GradosEntrada();
            Console.WriteLine($"{"Vértice",-26}{"Salida",8}{"Entrada",9}{"Total",8}");
            foreach (var v in g.Vertices.OrderByDescending(v => g.Vecinos(v).Count + entrada[v]))
                Console.WriteLine($"{v,-26}{g.Vecinos(v).Count,8}{entrada[v],9}{g.Vecinos(v).Count + entrada[v],8}");
        }
        else
        {
            Console.WriteLine($"{"Vértice",-26}{"Grado",8}");
            foreach (var v in g.Vertices.OrderByDescending(v => g.Vecinos(v).Count))
                Console.WriteLine($"{v,-26}{g.Vecinos(v).Count,8}");
        }
    }

    public static void ListaAdyacencia(Grafo g)
    {
        Console.WriteLine($"\n=== LISTA DE ADYACENCIA: {g.Nombre} ===");
        string flecha = g.Dirigido ? "->" : "--";
        foreach (var v in g.Vertices)
        {
            var vecinos = g.Vecinos(v).Select(a => g.Ponderado ? $"{a.Destino} ({a.Peso:0.##})" : a.Destino);
            string texto = vecinos.Any() ? string.Join(", ", vecinos) : "(sin conexiones)";
            Console.WriteLine($"{v} {flecha} {texto}");
        }
    }

    public static void MatrizAdyacencia(Grafo g)
    {
        const int Limite = 20;
        if (g.NumVertices > Limite)
        {
            Console.WriteLine($"La matriz solo se muestra para grafos de hasta {Limite} vértices (este tiene {g.NumVertices}).");
            return;
        }

        var lista = g.Vertices.ToList();
        Console.WriteLine($"\n=== MATRIZ DE ADYACENCIA: {g.Nombre} ===");
        for (int i = 0; i < lista.Count; i++) Console.WriteLine($"[{i,2}] {lista[i]}");

        Console.WriteLine();
        Console.Write("     ");
        for (int j = 0; j < lista.Count; j++) Console.Write($"{j,5}");
        Console.WriteLine();

        for (int i = 0; i < lista.Count; i++)
        {
            var pesos = g.Vecinos(lista[i]).ToDictionary(a => a.Destino, a => a.Peso);
            Console.Write($"[{i,2}] ");
            foreach (var destino in lista)
                Console.Write(pesos.TryGetValue(destino, out double p) ? $"{p,5:0.##}" : $"{"-",5}");
            Console.WriteLine();
        }
    }

    public static void ListaAristas(Grafo g)
    {
        Console.WriteLine($"\n=== ARISTAS: {g.Nombre} ({g.NumAristas}) ===");
        string flecha = g.Dirigido ? "->" : "--";
        int i = 1;
        foreach (var a in g.TodasLasAristas())
            Console.WriteLine($"{i++,3}. {a.Origen} {flecha} {a.Destino}" + (g.Ponderado ? $"   [{a.Peso:0.##}]" : ""));
    }
}
