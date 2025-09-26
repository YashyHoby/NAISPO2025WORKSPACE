using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using HeartScape.IO.Gateway;

public class MicrocontrollerWebSocketClient : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] string host = "127.0.0.1";
    [SerializeField] int port = 8765;
    [SerializeField] bool connectOnEnable = true;
    [SerializeField, Min(0.5f)] float reconnectDelaySeconds = 3f;

    [Header("Mapping")]
    [SerializeField] float cvInputMax = 0.2f;
    [SerializeField] float pressTimeMax = 2.0f;
    [SerializeField] HeartGateway targetGateway;

    CancellationTokenSource cancellation;
    Task clientTask;

    void Awake()
    {
        if (targetGateway == null)
        {
            targetGateway = FindFirstObjectByType<HeartGateway>();
        }
    }

    void OnEnable()
    {
        if (connectOnEnable)
        {
            StartClient();
        }
    }

    void OnDisable()
    {
        StopClient();
    }

    public void StartClient()
    {
        if (targetGateway == null)
        {
            Debug.LogError("[MicrocontrollerWebSocketClient] HeartGateway is not assigned.");
            return;
        }

        if (clientTask != null && !clientTask.IsCompleted)
        {
            return;
        }

        cancellation = new CancellationTokenSource();
        clientTask = RunClientAsync(cancellation.Token);
    }

    public void StopClient()
    {
        if (cancellation == null) return;
        cancellation.Cancel();
        cancellation.Dispose();
        cancellation = null;
        clientTask = null;
    }

    async Task RunClientAsync(CancellationToken token)
    {
        var uri = new Uri($"ws://{host}:{port}/");

        while (!token.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            try
            {
                Debug.Log($"[MicrocontrollerWebSocketClient] Connecting to {uri}...");
                await socket.ConnectAsync(uri, token);
                Debug.Log("[MicrocontrollerWebSocketClient] Connected.");
                await ReceiveLoopAsync(socket, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MicrocontrollerWebSocketClient] Connection error: {ex.Message}");
            }

            if (token.IsCancellationRequested) break;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Debug.Log("[MicrocontrollerWebSocketClient] Client loop stopped.");
    }

    async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[2048];

        while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            string payload = await ReceiveMessageAsync(socket, buffer, token);
            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            ProcessMessage(payload.Trim());
        }
    }

    static async Task<string> ReceiveMessageAsync(ClientWebSocket socket, byte[] buffer, CancellationToken token)
    {
        var builder = new StringBuilder();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                return null;
            }

            if (result.Count > 0)
            {
                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        }
        while (!result.EndOfMessage);

        return builder.ToString();
    }

    void ProcessMessage(string json)
    {
        MicrocontrollerPayload payload;
        try
        {
            payload = JsonUtility.FromJson<MicrocontrollerPayload>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MicrocontrollerWebSocketClient] Failed to parse JSON: {ex.Message}\n{json}");
            return;
        }

        if (payload == null)
        {
            Debug.LogWarning("[MicrocontrollerWebSocketClient] Parsed payload was null.");
            return;
        }

        if (targetGateway == null)
        {
            Debug.LogWarning("[MicrocontrollerWebSocketClient] HeartGateway is not available.");
            return;
        }

        try
        {
            var input = TranslatePayload(payload);
            targetGateway.Inject(input);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MicrocontrollerWebSocketClient] Failed to translate payload: {ex.Message}");
        }
    }

    HeartGateway.HeartInput TranslatePayload(MicrocontrollerPayload payload)
    {
        var manager = targetGateway != null ? targetGateway.manager : null;
        var mapper = manager != null ? manager.appearance : null;

        Vector2 hrRange = mapper != null ? mapper.hrRange : new Vector2(50f, 120f);
        Vector2 cvRange = mapper != null ? mapper.cvRange : new Vector2(0.05f, 0.5f);
        Vector2 rngRange = mapper != null ? mapper.rngRange : new Vector2(10f, 35f);
        Vector2 meanRange = mapper != null ? mapper.meanRange : new Vector2(65f, 95f);

        float hr = Mathf.Clamp(payload.hr, hrRange.x, hrRange.y);
        float cvNormalized = Mathf.Clamp01(payload.cv / Mathf.Max(0.0001f, cvInputMax));
        float cv = Mathf.Lerp(cvRange.x, cvRange.y, cvNormalized);
        float range = Mathf.Lerp(rngRange.x, rngRange.y, Mathf.Clamp01(payload.dynRange));
        float hrNorm = Mathf.InverseLerp(hrRange.x, hrRange.y, hr);
        float mean = Mathf.Lerp(meanRange.x, meanRange.y, Mathf.Clamp01(hrNorm));
        float clampedPressTime = Mathf.Clamp(payload.time, 0f, pressTimeMax);
        float hueNormalized = Mathf.Repeat(payload.Hue, 360f) / 360f;

        string logicalId = $"BTN_{Mathf.Clamp(payload.button, 0, 3)}";

        return new HeartGateway.HeartInput
        {
            uid = logicalId,
            switchNo = Mathf.Clamp(payload.button, 0, 3) + 1,
            hr = hr,
            cv = cv,
            range = range,
            mean = mean,
            pressTime = clampedPressTime,
            dynRange = payload.dynRange,
            hue = hueNormalized,
            hasHue = true
        };
    }

    [Serializable]
    class MicrocontrollerPayload
    {
        public int button;
        public string state;
        public float time;
        public float hr;
        public float cv;
        public float dynRange;
        public float Hue;
    }
}
