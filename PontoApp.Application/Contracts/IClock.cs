namespace PontoApp.Application.Contracts;
public interface IClock {
    /// <summary>Agora de S�o Paulo (sem fuso acoplado). Use como "hora local" do sistema.</summary>
    DateTime NowSp();
}
