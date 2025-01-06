import * as signalR from '@microsoft/signalr';
import './style.css';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5050/discountcodeshub', {
    withCredentials: false,
  })
  .configureLogging(signalR.LogLevel.Information)
  .build();

const codeCountInput = document.getElementById('codeCount') as HTMLInputElement;
const codeLengthInput = document.getElementById('codeLength') as HTMLInputElement;
const generateButton = document.getElementById('generateButton') as HTMLButtonElement;
const codeInput = document.getElementById('codeInput') as HTMLInputElement;
const useCodeButton = document.getElementById('useCodeButton') as HTMLButtonElement;
const messagesDiv = document.getElementById('messages') as HTMLDivElement;
const connectionStatus = document.getElementById('connectionStatus') as HTMLDivElement;

function updateConnectionStatus(isConnected: boolean) {
  const dot = connectionStatus.querySelector('span:first-child') as HTMLSpanElement;
  const text = connectionStatus.querySelector('span:last-child') as HTMLSpanElement;
  
  if (isConnected) {
    dot.className = 'w-3 h-3 rounded-full bg-green-500';
    text.textContent = 'Connected';
  } else {
    dot.className = 'w-3 h-3 rounded-full bg-red-500';
    text.textContent = 'Disconnected';
  }
}

function addMessage(message: string, type: 'sent' | 'received') {
  const messageElement = document.createElement('div');
  messageElement.className = `mb-2 p-2 rounded-lg ${
    type === 'sent' 
      ? 'bg-blue-500 text-white ml-auto max-w-[80%] text-right' 
      : 'bg-gray-200 mr-auto max-w-[80%]'
  }`;
  messageElement.textContent = type === 'sent' ? `${message} →` : `→ ${message}`;
  messagesDiv.appendChild(messageElement);
  messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

async function start() {
  try {
    await connection.start();
    console.log('Connected to SignalR Hub');
    updateConnectionStatus(true);
    generateButton.disabled = false;
    useCodeButton.disabled = false;
  } catch (err) {
    console.error('Error connecting to SignalR Hub:', err);
    updateConnectionStatus(false);
    generateButton.disabled = true;
    useCodeButton.disabled = true;
    setTimeout(start, 5000);
  }
}

connection.onclose(async () => {
  updateConnectionStatus(false);
  generateButton.disabled = true;
  useCodeButton.disabled = true;
  await start();
});

generateButton.addEventListener('click', async () => {
  const count = parseInt(codeCountInput.value);
  const length = parseInt(codeLengthInput.value);

  if (count < 1 || length < 7 || length > 8) {
    addMessage(`Invalid input: Count must be positive, length must be 7 or 8`, 'received');
    return;
  }

  try {
    addMessage(`Generating ${count} codes of length ${length}...`, 'sent');
    const response = await connection.invoke('GenerateCodes', { count, length });
    
    if (response.result) {
      addMessage(`Successfully generated ${count} codes`, 'received');
    } else {
      addMessage('Failed to generate codes. Please check your inputs.', 'received');
    }
  } catch (err) {
    console.error('Error generating codes:', err);
    addMessage('Error generating codes', 'received');
  }
});

useCodeButton.addEventListener('click', async () => {
  const code = codeInput.value.trim().toUpperCase();
  
  if (!code) {
    addMessage('Please enter a code', 'received');
    return;
  }

  try {
    addMessage(`Using code: ${code}`, 'sent');
    const response = await connection.invoke('UseCode', { code });
    
    if (response.result === 1) {
      addMessage('Code successfully used!', 'received');
    } else {
      addMessage('Invalid or already used code', 'received');
    }
    
    codeInput.value = '';
  } catch (err) {
    console.error('Error using code:', err);
    addMessage('Error using code', 'received');
  }
});

start();