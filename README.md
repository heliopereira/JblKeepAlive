# 🔊 JBL Keep-Alive Sentinel

**O sentinela definitivo para sua JBL Go 4 no Windows.**

Você já sentiu a frustração de estar em um momento de foco absoluto (*Deep Work*) e sua **JBL Go 4** simplesmente desligar por "inatividade" enquanto está conectada ao PC? Este projeto resolve esse problema de forma elegante, eficiente e totalmente automatizada.

## 🚀 A Solução "Outside the Box"

Diferente de soluções comuns que exigem tocar música em volume baixo, o **JBL Keep-Alive Sentinel** utiliza uma abordagem de engenharia de áudio:

* **Heartbeat Inaudível**: Gera uma onda senoidal de **20Hz** (limite inferior da audição humana) com ganho mínimo.
* **Arquitetura Reativa**: Utiliza as **Core Audio APIs** do Windows para monitorar eventos de hardware. O serviço só consome recursos quando a JBL está realmente ativa.
* **Zero Polling**: Esqueça loops infinitos de verificação. O sistema é notificado pelo SO no milissegundo em que a conexão ocorre.

## ✨ Funcionalidades

* **System Tray Integration**: Ícone dinâmico na bandeja do sistema para feedback visual instantâneo.
* **Notificações Nativas**: Balloon Tips avisam quando a proteção de conexão foi ativada.
* **Auto-Contido**: Publicado como um único executável de arquivo único (Single-file EXE).
* **PNG-to-Icon Engine**: Carregamento dinâmico de ícones com transparência a partir de recursos embutidos.

## 🛠 Tech Stack

* **.NET 10.0** (C#)
* **NAudio**: Manipulação de áudio de baixo nível e Core Audio API.
* **Windows Forms**: Interface leve para System Tray.
* **Microsoft.Extensions.Hosting**: Gerenciamento de ciclo de vida do Worker Service.

## 📦 Instalação Prática

Para implantar o sentinela na sua workstation:

1. **Clone o repositório:**
```bash
git clone https://github.com/heliopereira/JblKeepAlive.git

```


2. **Publique o binário:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

```


3. **Execute o Instalador:**
Rode o script `install.ps1` (ou o `deploy.bat`) como Administrador para registrar o atalho na inicialização do Windows.

## 🧠 Como Funciona? (Deep Dive)

O coração do sistema é o `IMMNotificationClient`. Em vez de perguntar ao Windows a cada segundo "A JBL está aí?", nós registramos um callback:

```csharp
// Exemplo da nossa abordagem reativa
public void OnDeviceStateChanged(string deviceId, DeviceState newState) {
    if (deviceId.Contains("JBL") && newState == DeviceState.Active) {
        StartHeartbeat(); // Inicia o sinal de 20Hz imediatamente
    }
}

```

Isso garante que sua **JBL Go 4** nunca entre em modo de economia de energia enquanto você estiver logado, economizando processamento do seu PC e preservando a vida útil do hardware.

---

## 👨‍💻 Autor

**Hélio Pereira** Desenvolvedor Fullstack .NET com foco em IA, IoT e infraestrutura Linux. Apaixonado por criar sistemas robustos que resolvem problemas reais do cotidiano tecnológico.

* **GitHub**: [heliopereira](https://github.com/heliopereira)
* **LinkedIn**: [Hélio Pereira](https://www.google.com/search?q=https://www.linkedin.com/in/heliopereira/)

---

*Este projeto foi desenvolvido com uma visão prática e orientada a resultados, explorando todo o potencial do ecossistema .NET.*
