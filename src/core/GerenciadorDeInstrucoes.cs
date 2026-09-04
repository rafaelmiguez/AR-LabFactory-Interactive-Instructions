using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

[System.Serializable]
public class PassoTutorial
{
    public string titulo;

    [TextArea(3, 10)]
    public string texto;

    [Tooltip("Lista de imagens deste passo. Deixe vazia para não mostrar nenhuma imagem.")]
    public List<Sprite> imagens = new List<Sprite>();

    [Tooltip("Lista de Pings para este passo. Deixe vazia para não mostrar nenhum ping.")]
    public List<int> idsDosPings = new List<int>();
}

[System.Serializable]
public class TarefaTutorial
{
    public string nomeDaTarefa = "Tarefa";
    public List<PassoTutorial> passos = new List<PassoTutorial>();
}

public class GerenciadorDeInstrucoes : MonoBehaviour
{
    private const string CHAVE_ANCORAS = "Tutorial_Drivolution";

    [Header("Catálogo de Pings (Coloque na mesma ordem do Calibrador!)")]
    public GameObject[] prefabsDePing;

    private readonly Dictionary<Guid, int> mapaDePings = new Dictionary<Guid, int>();
    private readonly Dictionary<Guid, int> mapaDeTipos = new Dictionary<Guid, int>();
    private readonly List<GameObject> pingsAtivos = new List<GameObject>();

    [Header("Widgets / Telas")]
    public GameObject widgetSelecaoUsuario;
    public GameObject widgetSelecaoTarefa;
    public GameObject widgetInstrucoes;

    [Header("Conexões da Interface (UI)")]
    public GameObject areaDeConteudo;
    public TextMeshProUGUI uiTitulo;
    public TextMeshProUGUI uiInstrucao;
    public Image[] uiImagens;
    public GameObject botaoAvancar;
    public GameObject botaoSair;
    public ScrollRect meuScroll;

    [Header("Conteúdo do Tutorial")]
    public List<TarefaTutorial> tarefas = new List<TarefaTutorial>();

    [Header("Log da Sessão")]
    public string nomeArquivoLog = "registro_sessão_drivoltion.txt";

    private int indiceTarefaAtual = -1;
    private int instrucaoAtual = 1;
    private string usuarioSelecionado = string.Empty;
    private string caminhoArquivoLog;
    private bool ultimoPassoJaFoiRegistrado = false;

    async void Start()
    {
        caminhoArquivoLog = Path.Combine(Application.persistentDataPath, nomeArquivoLog);
        GravarLog("-------------------------");
        GravarLog("Aplicação aberta");

        MostrarSomenteSelecaoDeUsuario();
        EsconderTodosOsPings();

        await CarregarPingsDaFabrica();
    }

    void MostrarSomenteSelecaoDeUsuario()
    {
        if (widgetSelecaoUsuario != null) widgetSelecaoUsuario.SetActive(true);
        if (widgetSelecaoTarefa != null) widgetSelecaoTarefa.SetActive(false);
        if (widgetInstrucoes != null) widgetInstrucoes.SetActive(false);
    }

    void MostrarSomenteSelecaoDeTarefa()
    {
        if (widgetSelecaoUsuario != null) widgetSelecaoUsuario.SetActive(false);
        if (widgetSelecaoTarefa != null) widgetSelecaoTarefa.SetActive(true);
        if (widgetInstrucoes != null) widgetInstrucoes.SetActive(false);
    }

    void MostrarSomenteInstrucoes()
    {
        if (widgetSelecaoUsuario != null) widgetSelecaoUsuario.SetActive(false);
        if (widgetSelecaoTarefa != null) widgetSelecaoTarefa.SetActive(false);
        if (widgetInstrucoes != null) widgetInstrucoes.SetActive(true);
    }

    public void SelecionarUsuario(string nomeUsuario)
    {
        if (string.IsNullOrWhiteSpace(nomeUsuario))
        {
            Debug.LogWarning("Nome de usuário vazio na seleção.");
            return;
        }

        usuarioSelecionado = nomeUsuario.Trim();
        GravarLog("Usuário " + usuarioSelecionado + " selecionado");

        MostrarSomenteSelecaoDeTarefa();
        EsconderTodosOsPings();
    }
    public void SelecionarUsuarioPeloTexto(TextMeshProUGUI textoUsuario)
    {
        if (textoUsuario == null)
        {
            Debug.LogWarning("Texto TMP do usuário não foi atribuído no botão.");
            return;
        }

        string nomeUsuario = textoUsuario.text;

        if (string.IsNullOrWhiteSpace(nomeUsuario))
        {
            Debug.LogWarning("O texto do botão de usuário está vazio.");
            return;
        }

        SelecionarUsuario(nomeUsuario.Trim());
    }
    public void IniciarTarefaEspecifica(int indexDaTarefa)
    {
        if (indexDaTarefa < 0 || indexDaTarefa >= tarefas.Count)
        {
            Debug.LogWarning("Índice de tarefa inválido: " + indexDaTarefa);
            return;
        }

        if (tarefas[indexDaTarefa] == null || tarefas[indexDaTarefa].passos == null || tarefas[indexDaTarefa].passos.Count == 0)
        {
            Debug.LogWarning("A tarefa selecionada não possui passos configurados.");
            return;
        }

        indiceTarefaAtual = indexDaTarefa;
        instrucaoAtual = 1;
        ultimoPassoJaFoiRegistrado = false;

        GravarLog("Tarefa selecionada: " + tarefas[indiceTarefaAtual].nomeDaTarefa);

        AtualizarPainelEPings();
        MostrarSomenteInstrucoes();
    }

    public void AvancarInstrucao()
    {
        if (!TemPassoAtual()) return;

        RegistrarPassoAtualNoLog();

        if (instrucaoAtual < QuantidadeDePassosDaTarefaAtual())
        {
            instrucaoAtual++;
            ultimoPassoJaFoiRegistrado = false;
            AtualizarPainelEPings();
        }
        else
        {
            FinalizarTarefaAtual();
        }
    }

    public void VoltarInstrucao()
    {
        if (!TemPassoAtual()) return;

        if (instrucaoAtual > 1)
        {
            instrucaoAtual--;
            ultimoPassoJaFoiRegistrado = false;
            AtualizarPainelEPings();
        }
    }

    public void RetornarAoMenu()
    {
        if (widgetInstrucoes != null) widgetInstrucoes.SetActive(false);
        if (widgetSelecaoUsuario != null) widgetSelecaoUsuario.SetActive(false);
        if (widgetSelecaoTarefa != null) widgetSelecaoTarefa.SetActive(true);

        EsconderTodosOsPings();
    }

    public void AlternarVisibilidade()
    {
        if (!TemPassoAtual()) return;

        bool esconder = uiTitulo != null && uiTitulo.gameObject.activeSelf;

        if (uiTitulo != null) uiTitulo.gameObject.SetActive(!esconder);
        if (uiInstrucao != null) uiInstrucao.gameObject.SetActive(!esconder);

        if (uiImagens == null) return;

        if (esconder)
        {
            foreach (Image imagem in uiImagens)
            {
                if (imagem != null) imagem.gameObject.SetActive(false);
            }
        }
        else
        {
            PassoTutorial passoAtual = ObterPassoAtual();
            ConfigurarImagensDoPasso(passoAtual.imagens);
        }
    }

    public void SairDoTutorial()
    {
        if (TemPassoAtual() && widgetInstrucoes != null && widgetInstrucoes.activeSelf)
        {
            RegistrarPassoAtualNoLog();
            GravarLog("Treinamento finalizado com sucesso");
        }

        GravarLog("Usuário saiu do aplicativo");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void FinalizarTarefaAtual()
    {
        GravarLog("Treinamento finalizado com sucesso");

        if (uiTitulo != null) uiTitulo.text = string.Empty;
        if (uiInstrucao != null) uiInstrucao.text = "Tarefa Concluída com Sucesso!";

        EsconderTodasAsImagens();
        EsconderTodosOsPings();

        if (botaoAvancar != null) botaoAvancar.SetActive(false);
        if (botaoSair != null) botaoSair.SetActive(true);
    }

    void RegistrarPassoAtualNoLog()
    {
        if (!TemPassoAtual() || ultimoPassoJaFoiRegistrado) return;

        string passoLimpo = LimparTextoParaLog(ObterPassoAtual().texto);
        GravarLog("Opção " + instrucaoAtual + " realizada: " + passoLimpo);
        ultimoPassoJaFoiRegistrado = true;
    }

    string LimparTextoParaLog(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;

        return texto
            .Replace("\n", " ")
            .Replace("\r", string.Empty)
            .Replace("Avance apenas se completou a tarefa.", string.Empty)
            .Trim();
    }

    void AtualizarPainelEPings()
    {
        if (!TemPassoAtual())
        {
            Debug.LogWarning("Não há passo atual válido para atualizar.");
            return;
        }

        PassoTutorial passoAtual = ObterPassoAtual();

        foreach (GameObject ping in pingsAtivos)
        {
            bool deveEstarVisivel = false;

            if (passoAtual.idsDosPings != null)
            {
                foreach (int idDesejado in passoAtual.idsDosPings)
                {
                    if (ping != null && ping.name == "Ping_Gravado_" + idDesejado)
                    {
                        deveEstarVisivel = true;
                        break;
                    }
                }
            }

            if (ping != null) ping.SetActive(deveEstarVisivel);
        }

        bool estaNoUltimoPasso = instrucaoAtual == QuantidadeDePassosDaTarefaAtual();

        if (botaoAvancar != null) botaoAvancar.SetActive(!estaNoUltimoPasso);
        if (botaoSair != null) botaoSair.SetActive(estaNoUltimoPasso);

        if (uiTitulo != null) uiTitulo.text = passoAtual.titulo;
        if (uiInstrucao != null) uiInstrucao.text = passoAtual.texto;

        ConfigurarImagensDoPasso(passoAtual.imagens);

        if (meuScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            meuScroll.verticalNormalizedPosition = 1f;
        }
    }

    void ConfigurarImagensDoPasso(List<Sprite> imagensDoPasso)
    {
        EsconderTodasAsImagens();

        if (uiImagens == null || imagensDoPasso == null) return;

        int quantidade = Mathf.Min(uiImagens.Length, imagensDoPasso.Count);

        for (int i = 0; i < quantidade; i++)
        {
            if (uiImagens[i] == null || imagensDoPasso[i] == null) continue;

            uiImagens[i].sprite = imagensDoPasso[i];
            uiImagens[i].gameObject.SetActive(true);
        }
    }

    void EsconderTodasAsImagens()
    {
        if (uiImagens == null) return;

        foreach (Image imagem in uiImagens)
        {
            if (imagem != null) imagem.gameObject.SetActive(false);
        }
    }

    PassoTutorial ObterPassoAtual()
    {
        return tarefas[indiceTarefaAtual].passos[instrucaoAtual - 1];
    }

    int QuantidadeDePassosDaTarefaAtual()
    {
        if (indiceTarefaAtual < 0 || indiceTarefaAtual >= tarefas.Count) return 0;
        if (tarefas[indiceTarefaAtual] == null || tarefas[indiceTarefaAtual].passos == null) return 0;
        return tarefas[indiceTarefaAtual].passos.Count;
    }

    bool TemPassoAtual()
    {
        if (indiceTarefaAtual < 0 || indiceTarefaAtual >= tarefas.Count) return false;
        if (tarefas[indiceTarefaAtual] == null || tarefas[indiceTarefaAtual].passos == null) return false;
        if (tarefas[indiceTarefaAtual].passos.Count == 0) return false;
        return instrucaoAtual > 0 && instrucaoAtual <= tarefas[indiceTarefaAtual].passos.Count;
    }

    void GravarLog(string acao)
    {
        try
        {
            string dataHora = DateTime.Now.ToString("yyyy MM dd HH:mm 'GMT'");
            string linhaFinal = acao.Contains("---") ? acao : $"{dataHora} {acao}";

            using (StreamWriter writer = new StreamWriter(caminhoArquivoLog, true))
            {
                writer.WriteLine(linhaFinal);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao salvar log: " + e.Message);
        }
    }

    async Task CarregarPingsDaFabrica()
    {
        string dados = PlayerPrefs.GetString(CHAVE_ANCORAS, "");
        if (string.IsNullOrEmpty(dados)) return;

        string[] paresDeDados = dados.Split(',');
        List<Guid> listaDeGuids = new List<Guid>();

        foreach (string par in paresDeDados)
        {
            string[] info = par.Split(':');
            if (info.Length < 2) continue;

            int idPingGravado = int.Parse(info[0]);
            int tipoGravado = 0;
            if (info.Length > 2) tipoGravado = int.Parse(info[2]);

            if (Guid.TryParse(info[1], out Guid guid))
            {
                mapaDePings[guid] = idPingGravado;
                mapaDeTipos[guid] = tipoGravado;
                listaDeGuids.Add(guid);
            }
        }

        var ancorasEncontradas = new List<OVRSpatialAnchor.UnboundAnchor>();
        var resultado = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(listaDeGuids, ancorasEncontradas);

        if (!resultado.Success) return;

        foreach (var ancoraInvisivel in ancorasEncontradas)
        {
            await ancoraInvisivel.LocalizeAsync();

            int tipoDestePing = mapaDeTipos[ancoraInvisivel.Uuid];
            if (tipoDestePing < 0 || tipoDestePing >= prefabsDePing.Length) continue;

            GameObject novoPing = Instantiate(prefabsDePing[tipoDestePing]);
            novoPing.SetActive(false);

            OVRSpatialAnchor ancoraFisica = novoPing.AddComponent<OVRSpatialAnchor>();
            ancoraInvisivel.BindTo(ancoraFisica);

            int idPing = mapaDePings[ancoraInvisivel.Uuid];
            novoPing.name = "Ping_Gravado_" + idPing;
            pingsAtivos.Add(novoPing);
        }

        if (widgetInstrucoes != null && widgetInstrucoes.activeSelf && TemPassoAtual())
        {
            AtualizarPainelEPings();
        }
        else
        {
            EsconderTodosOsPings();
        }
    }

    public void EsconderTodosOsPings()
    {
        foreach (GameObject ping in pingsAtivos)
        {
            if (ping != null) ping.SetActive(false);
        }
    }
}
