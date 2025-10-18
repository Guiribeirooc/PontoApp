namespace PontoApp.Application.Contracts;
public interface IClock {
    /// <summary>Agora de São Paulo (sem fuso acoplado). Use como "hora local" do sistema.</summary>
    DateTime NowSp();
}
