# 🔊 JBL Keep-Alive Sentinel

**O sentinela definitivo para caixas de som Bluetooth no Windows.**

Você já estava em foco total e sua **JBL Go 4** simplesmente desligou por "inatividade" enquanto conectada ao PC? Este projeto resolve isso de forma elegante, eficiente e totalmente automatizada.

## 🚀 A Solução

Diferente de soluções que exigem tocar música em volume baixo, o **JBL Keep-Alive Sentinel** usa engenharia de áudio:

- **Heartbeat Inaudível**: Gera uma onda senoidal de **20Hz** (limite inferior da audição humana) com ganho mínimo de `0.005` — imperceptível ao ouvido, suficiente para manter o dispositivo ativo.
- **Arquitetura Reativa**: Usa as **Core Audio APIs** do Windows via `IMMNotificationClient`. O serviço é notificado pelo SO no momento exato em que o dispositivo conecta ou desconecta.
- **Fallback Periódico**: Um loop de 5 segundos garante detecção mesmo quando o Windows não dispara o evento de hardware.
- **Reconexão Automática**: Detecta e reinicia o heartbeat automaticamente após desligar e religar o dispositivo.

## ✨ Funcionalidades

- **System Tray**: Ícone dinâmico na bandeja do sistema com feedback visual instantâneo (conectado/desconectado).
- **Notificações Nativas**: Balloon tip ao conectar o dispositivo.
- **Instância Única**: Proteção via Mutex — rodar o executável duas vezes não cria dois heartbeats.
- **Totalmente Configurável**: Nome do dispositivo, mensagens da UI e comportamento definidos via `appsettings.json`.
- **Compatível com qualquer modelo**: Não é exclusivo para JBL Go 4 — funciona com qualquer dispositivo de áudio Bluetooth.
- **Single-file EXE**: Publicado como executável auto-contido, sem dependências externas.

## 🛠 Tech Stack

- **.NET 10.0** (C#)
- **NAudio 2.2.1**: Enumeração de dispositivos, Core Audio API e geração de sinal.
- **Windows Forms**: Interface leve para System Tray.
- **Microsoft.Extensions.Hosting**: Ciclo de vida do Worker Service e injeção de dependência.

## ⚙️ Configuração

Todas as configurações ficam em `appsettings.json`:

```json
{
  "JblDevice": {
    "NameFilter": "JBL Go 4",
    "DisplayName": "JBL Go 4"
  },
  "UI": {
    "AppName": "JBL Sentinel",
    "ConnectedMessage": "{0}: Ativa e Protegida",
    "DisconnectedMessage": "{0}: Não detectada",
    "BalloonTipTitle": "{0}",
    "BalloonTipMessage": "Caixa conectada. O heartbeat de áudio está ativo."
  }
}
```

Para usar com outro dispositivo, basta alterar `NameFilter` com parte do nome que aparece em **Configurações → Som → Dispositivos de saída**:

```json
"JblDevice": {
  "NameFilter": "Sony WH-1000XM5",
  "DisplayName": "Meu Fone Sony"
}
```

## 📦 Instalação

### Opção 1 — Script automático (recomendado)

Execute o `install.ps1` no PowerShell. Ele publica o binário, instala em `%LocalAppData%\JblKeepAlive` e cria um atalho na pasta Startup do Windows para iniciar com o sistema:

```powershell
.\install.ps1
```

### Opção 2 — Manual

1. Clone o repositório:
```bash
git clone https://github.com/heliopereira/JblKeepAlive.git
```

2. Publique o binário:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

3. Copie o executável para onde preferir e adicione ao Startup do Windows manualmente.

## 🧠 Como Funciona

O núcleo é o `Worker`, um `BackgroundService` que orquestra dois mecanismos:

**1. Notificações reativas (IMMNotificationClient)**

```csharp
// AudioNotificationClient.cs
public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    => _onChanged.Invoke(); // dispara UpdateHeartbeatState imediatamente
```

**2. Máquina de estados no UpdateHeartbeatState**

| Estado do dispositivo | Heartbeat | Ação |
|---|---|---|
| `Active` + heartbeat não tocando | qualquer | Para o anterior, inicia novo |
| `Active` + heartbeat tocando | Playing | Mantém, atualiza status |
| `Unplugged` + playback parou | Stopped | Para heartbeat imediatamente |
| `Unplugged` + playback ativo | Playing | Aguarda grace period de 10s |
| `NotPresent` / `Disabled` / `null` | qualquer | Para heartbeat |

**3. Proteção contra deadlock**

O `OnPlaybackStopped` do NAudio dispara em uma thread interna. Para evitar deadlock com o `lock` do loop principal, ele não adquire o lock — apenas limpa o `_waveOut` e sinaliza `IsConnected = false`. O próximo ciclo do loop detecta e reage.

## 🗂 Estrutura do Projeto

```
JblKeepAlive/
├── Program.cs                  # Bootstrap: host, DI, mutex de instância única, tray
├── Worker.cs                   # Núcleo: monitora dispositivos, gerencia heartbeat
├── AudioNotificationClient.cs  # Callback de eventos de hardware do Windows
├── JblStatusService.cs         # Singleton de estado compartilhado entre Worker e UI
├── TrayApplicationContext.cs   # UI da bandeja: ícone, tooltip, balloon tip, menu
├── AppSettings.cs              # Classes de configuração tipadas
├── appsettings.json            # Configuração padrão
└── install.ps1                 # Script de instalação e registro no Startup
```

---

## 👨‍💻 Autor

**Hélio Pereira** — Desenvolvedor Fullstack .NET com foco em IA, IoT e infraestrutura Linux.

- **GitHub**: [heliopereira](https://github.com/heliopereira)
- **LinkedIn**: [Hélio Pereira](https://www.linkedin.com/in/heliopereira/)
