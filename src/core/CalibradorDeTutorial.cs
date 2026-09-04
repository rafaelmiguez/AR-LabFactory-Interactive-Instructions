using UnityEngine;
using UnityEngine.Android;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CalibradorDeTutorial : MonoBehaviour
{
    private const string CHAVE_ANCORAS = "Tutorial_Drivolution";
    private const string NOME_ARQUIVO_DEBUG = "debug.txt";
    private const string PERMISSAO_SCENE = "com.oculus.permission.USE_SCENE";

    [Header("Catálogo de Pings")]
    public GameObject[] prefabsDePing;
    public Transform maoDireita;

    [Header("Ajuste de Posição")]
    [Tooltip("Distância em metros para frente do controle (0.15 = 15cm)")]
    public float distanciaParaFrente = 0.15f;

    [Header("Limpeza de Tela (Modo Calibração)")]
    [Tooltip("Arraste para cá os painéis e widgets que devem sumir durante a calibração.")]
    public GameObject[] widgetsParaOcultar;

    private int indicePrefabAtual = 0;
    private GameObject fantasmaAtual;
    private int contadorDePings = 1;
    private readonly List<GameObject> pingsNaCena = new List<GameObject>();
    private string caminhoArquivoDebug;

    void Start()
    {
        caminhoArquivoDebug = Path.Combine(Application.persistentDataPath, NOME_ARQUIVO_DEBUG);

        GravarDebug("--------------------------------------------------");
        GravarDebug("CalibradorDeTutorial iniciado.");
        GravarDebug("Application.persistentDataPath = " + Application.persistentDataPath);
        GravarDebug("Arquivo de debug = " + caminhoArquivoDebug);
        GravarDebug("Chave de âncoras = " + CHAVE_ANCORAS);
        GravarDebug("Permissão Scene já concedida? " + Permission.HasUserAuthorizedPermission(PERMISSAO_SCENE));
        GravarDebug("Dados atuais da chave = " + PlayerPrefs.GetString(CHAVE_ANCORAS, "<vazio>"));

        contadorDePings = ObterProximoIdDisponivel();
        GravarDebug("Próximo ID disponível calculado = " + contadorDePings);

        if (widgetsParaOcultar != null)
        {
            foreach (GameObject widget in widgetsParaOcultar)
            {
                if (widget != null)
                {
                    widget.SetActive(false);
                    GravarDebug("Widget ocultado no Start: " + widget.name);
                }
            }
        }

        if (maoDireita == null)
        {
            GravarDebug("ERRO: 'maoDireita' não foi atribuída no Inspector.");
        }

        if (prefabsDePing == null || prefabsDePing.Length == 0)
        {
            GravarDebug("ERRO: 'prefabsDePing' está vazio.");
        }
        else
        {
            GravarDebug("Quantidade de prefabsDePing = " + prefabsDePing.Length);
        }

        AtualizarFantasma();
    }

    void Update()
    {
        if (maoDireita == null) return;

        Vector3 posicaoAlvo = maoDireita.position + (maoDireita.forward * distanciaParaFrente);

        if (fantasmaAtual != null)
        {
            fantasmaAtual.transform.position = posicaoAlvo;
            fantasmaAtual.transform.rotation = maoDireita.rotation;
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            MudarPrefab(1);
        }

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            GravarDebug("Botão A pressionado. Iniciando fluxo de salvamento. ID atual = " + contadorDePings);
            PosicionarESalvarPing(posicaoAlvo);
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            GravarDebug("Botão B pressionado. Limpando memória do app.");
            ApagarMemoriaDoQuest();
        }
    }

    void MudarPrefab(int direcao)
    {
        if (prefabsDePing == null || prefabsDePing.Length == 0)
        {
            GravarDebug("MudarPrefab ignorado: lista de prefabs vazia.");
            return;
        }

        indicePrefabAtual += direcao;

        if (indicePrefabAtual >= prefabsDePing.Length) indicePrefabAtual = 0;
        if (indicePrefabAtual < 0) indicePrefabAtual = prefabsDePing.Length - 1;

        string nomePrefab = prefabsDePing[indicePrefabAtual] != null ? prefabsDePing[indicePrefabAtual].name : "<null>";
        GravarDebug("Prefab alterado. Índice atual = " + indicePrefabAtual + " | Nome = " + nomePrefab);

        AtualizarFantasma();
    }

    void AtualizarFantasma()
    {
        if (fantasmaAtual != null)
        {
            Destroy(fantasmaAtual);
            fantasmaAtual = null;
        }

        if (maoDireita == null)
        {
            GravarDebug("AtualizarFantasma abortado: 'maoDireita' está nula.");
            return;
        }

        if (prefabsDePing == null || prefabsDePing.Length == 0)
        {
            GravarDebug("AtualizarFantasma abortado: sem prefabs configurados.");
            return;
        }

        if (indicePrefabAtual < 0 || indicePrefabAtual >= prefabsDePing.Length)
        {
            GravarDebug("AtualizarFantasma abortado: índice de prefab inválido = " + indicePrefabAtual);
            return;
        }

        if (prefabsDePing[indicePrefabAtual] == null)
        {
            GravarDebug("AtualizarFantasma abortado: prefab atual está nulo no índice " + indicePrefabAtual);
            return;
        }

        Vector3 posInicial = maoDireita.position + (maoDireita.forward * distanciaParaFrente);
        fantasmaAtual = Instantiate(prefabsDePing[indicePrefabAtual], posInicial, maoDireita.rotation);

        GravarDebug("Fantasma atualizado. Prefab = " + prefabsDePing[indicePrefabAtual].name +
                    " | Posição = " + posInicial);
    }

    async void PosicionarESalvarPing(Vector3 posicaoFinal)
    {
        bool permissaoOk = await GarantirPermissaoSceneAsync();
        if (!permissaoOk)
        {
            GravarDebug("Abortado: permissão USE_SCENE não concedida.");
            return;
        }

        if (prefabsDePing == null || prefabsDePing.Length == 0)
        {
            GravarDebug("PosicionarESalvarPing abortado: sem prefabs configurados.");
            return;
        }

        if (indicePrefabAtual < 0 || indicePrefabAtual >= prefabsDePing.Length || prefabsDePing[indicePrefabAtual] == null)
        {
            GravarDebug("PosicionarESalvarPing abortado: prefab atual inválido. Índice = " + indicePrefabAtual);
            return;
        }

        if (maoDireita == null)
        {
            GravarDebug("PosicionarESalvarPing abortado: 'maoDireita' está nula.");
            return;
        }

        GameObject novoPing = Instantiate(prefabsDePing[indicePrefabAtual], posicaoFinal, maoDireita.rotation);
        pingsNaCena.Add(novoPing);

        GravarDebug("Ping instanciado. ID pretendido = " + contadorDePings +
                    " | Tipo = " + indicePrefabAtual +
                    " | Nome do prefab = " + prefabsDePing[indicePrefabAtual].name +
                    " | Posição = " + posicaoFinal);

        OVRSpatialAnchor ancora = novoPing.AddComponent<OVRSpatialAnchor>();

        bool localizada = false;

        try
        {
            localizada = await ancora.WhenLocalizedAsync();
            GravarDebug("WhenLocalizedAsync = " + localizada + " | UUID = " + ancora.Uuid);
        }
        catch (Exception e)
        {
            GravarDebug("EXCEÇÃO no WhenLocalizedAsync: " + e.Message);
        }

        if (!localizada)
        {
            GravarDebug("FALHA: âncora não localizou antes do salvamento.");

            if (novoPing != null)
            {
                Destroy(novoPing);
                pingsNaCena.Remove(novoPing);
                GravarDebug("Ping temporário destruído após falha de localização.");
            }

            return;
        }

        object resultadoSaveObj = null;
        bool sucesso = false;
        string statusTexto = "<indisponível>";

        try
        {
            resultadoSaveObj = await ancora.SaveAnchorAsync();
            sucesso = ExtrairSuccess(resultadoSaveObj);
            statusTexto = ExtrairStatus(resultadoSaveObj);

            GravarDebug("Resultado do SaveAnchorAsync = " + sucesso +
                        " | Status = " + statusTexto +
                        " | UUID = " + ancora.Uuid);
        }
        catch (Exception e)
        {
            GravarDebug("EXCEÇÃO no SaveAnchorAsync: " + e.Message);
            sucesso = false;
            statusTexto = "EXCEPTION";
        }

        if (sucesso)
        {
            SalvarMapeamento(contadorDePings, ancora.Uuid.ToString(), indicePrefabAtual);

            GravarDebug("Ping salvo com sucesso. ID = " + contadorDePings +
                        " | UUID = " + ancora.Uuid +
                        " | Tipo = " + indicePrefabAtual);

            GravarDebug("Conteúdo final da chave após salvar = " + PlayerPrefs.GetString(CHAVE_ANCORAS, "<vazio>"));

            contadorDePings++;

            OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.RTouch);
            Invoke(nameof(PararVibracao), 0.2f);
        }
        else
        {
            GravarDebug("FALHA ao salvar a âncora. Status = " + statusTexto);

            if (novoPing != null)
            {
                Destroy(novoPing);
                pingsNaCena.Remove(novoPing);
                GravarDebug("Ping temporário destruído após falha no salvamento.");
            }
        }
    }

    async Task<bool> GarantirPermissaoSceneAsync()
    {
        bool jaTemPermissao = Permission.HasUserAuthorizedPermission(PERMISSAO_SCENE);
        GravarDebug("Checando permissão USE_SCENE. Já concedida? " + jaTemPermissao);

        if (jaTemPermissao)
            return true;

        GravarDebug("Solicitando permissão USE_SCENE em runtime...");

        try
        {
            Permission.RequestUserPermission(PERMISSAO_SCENE);
        }
        catch (Exception e)
        {
            GravarDebug("EXCEÇÃO ao pedir permissão USE_SCENE: " + e.Message);
            return false;
        }

        float timeout = 10f;
        float tempo = 0f;

        while (tempo < timeout)
        {
            await Task.Delay(250);
            tempo += 0.25f;

            if (Permission.HasUserAuthorizedPermission(PERMISSAO_SCENE))
            {
                GravarDebug("Permissão USE_SCENE concedida após request.");
                return true;
            }
        }

        GravarDebug("Permissão USE_SCENE NÃO concedida após request.");
        return false;
    }

    void SalvarMapeamento(int numeroDoPing, string uuid, int tipoDoPing)
    {
        string tutorialSalvo = PlayerPrefs.GetString(CHAVE_ANCORAS, "");
        string novoDado = numeroDoPing + ":" + uuid + ":" + tipoDoPing;

        GravarDebug("SalvarMapeamento chamado. Novo dado = " + novoDado);
        GravarDebug("Valor anterior da chave = " + (string.IsNullOrEmpty(tutorialSalvo) ? "<vazio>" : tutorialSalvo));

        if (string.IsNullOrEmpty(tutorialSalvo))
        {
            tutorialSalvo = novoDado;
        }
        else
        {
            tutorialSalvo += "," + novoDado;
        }

        PlayerPrefs.SetString(CHAVE_ANCORAS, tutorialSalvo);
        PlayerPrefs.Save();

        GravarDebug("Novo valor salvo na chave = " + tutorialSalvo);
    }

    void ApagarMemoriaDoQuest()
    {
        GravarDebug("ApagarMemoriaDoQuest chamado.");
        GravarDebug("Valor da chave antes de apagar = " + PlayerPrefs.GetString(CHAVE_ANCORAS, "<vazio>"));

        PlayerPrefs.DeleteKey(CHAVE_ANCORAS);
        PlayerPrefs.Save();

        contadorDePings = 1;

        foreach (GameObject ping in pingsNaCena)
        {
            if (ping != null) Destroy(ping);
        }

        pingsNaCena.Clear();

        if (fantasmaAtual != null)
        {
            Destroy(fantasmaAtual);
            fantasmaAtual = null;
        }

        AtualizarFantasma();

        GravarDebug("Chave apagada com sucesso.");
        GravarDebug("Valor da chave depois de apagar = " + PlayerPrefs.GetString(CHAVE_ANCORAS, "<vazio>"));
        GravarDebug("contadorDePings resetado para 1.");

        OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.RTouch);
        Invoke(nameof(PararVibracao), 0.5f);
    }

    void PararVibracao()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }

    int ObterProximoIdDisponivel()
    {
        string dados = PlayerPrefs.GetString(CHAVE_ANCORAS, "");

        if (string.IsNullOrEmpty(dados))
            return 1;

        string[] pares = dados.Split(',');
        int maiorId = 0;

        foreach (string par in pares)
        {
            if (string.IsNullOrWhiteSpace(par)) continue;

            string[] info = par.Split(':');
            if (info.Length < 1) continue;

            if (int.TryParse(info[0], out int id) && id > maiorId)
            {
                maiorId = id;
            }
        }

        return maiorId + 1;
    }

    bool ExtrairSuccess(object resultado)
    {
        if (resultado == null) return false;

        if (resultado is bool b)
            return b;

        try
        {
            var tipo = resultado.GetType();
            var propSuccess = tipo.GetProperty("Success");
            if (propSuccess != null && propSuccess.PropertyType == typeof(bool))
            {
                return (bool)propSuccess.GetValue(resultado);
            }
        }
        catch
        {
        }

        return false;
    }

    string ExtrairStatus(object resultado)
    {
        if (resultado == null) return "<null>";

        if (resultado is bool)
            return "<bool-sem-status>";

        try
        {
            var tipo = resultado.GetType();
            var propStatus = tipo.GetProperty("Status");
            if (propStatus != null)
            {
                object valor = propStatus.GetValue(resultado);
                return valor != null ? valor.ToString() : "<status-null>";
            }
        }
        catch
        {
        }

        return "<status-indisponível>";
    }

    void GravarDebug(string mensagem)
    {
        try
        {
            string dataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            using (StreamWriter writer = new StreamWriter(caminhoArquivoDebug, true))
            {
                writer.WriteLine("[" + dataHora + "] " + mensagem);
            }
        }
        catch
        {
        }
    }
}