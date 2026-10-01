/// <summary>Representa una conexión entre dos vértices.</summary>
public class Arista
{
	public string Origen { get; }
	public string Destino { get; }
	public double Peso { get; }

	public Arista(string origen, string destino, double peso)
	{
		Origen = origen;
		Destino = destino;
		Peso = peso;
	}
}
