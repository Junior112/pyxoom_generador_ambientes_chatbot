# Generador de Ambientes (Instancias) para Pyxoom-Rabbit

Herramienta para generar ambientes (instancias) de Pyxoom-Rabbit a partir de un único archivo de configuración. Pensado para equipos de soporte: ejecutas el .exe, se crean las carpetas por instancia con su `appsettings.json`, y se generan scripts para iniciar los procesos con PM2.

## Qué hace

- ✅ Genera múltiples instancias desde un solo `instances-config.json`
- ✅ Crea `appsettings.json` personalizados por instancia
- ✅ Copia todo el contenido de la carpeta publicada/compilada (`sourcePath`) a cada instancia
- ✅ Crea scripts de inicio por instancia (`start-*.bat` / `start-*.ps1`)
- ✅ Genera scripts PM2 para iniciar/actualizar procesos por instancia
- ✅ Permite campos dinámicos (ClientId, Serilog path, URLs, connection strings, etc.)

## Estructura del Proyecto

```
PyxoomInstanceGenerator/
├── Models/
│   ├── GeneratorConfig.cs          # Configuración principal del generador
│   └── InstanceConfiguration.cs    # Configuración de cada instancia
├── Services/
│   ├── AppSettingsGenerator.cs     # Generador de appsettings personalizados
│   ├── FileManager.cs              # Gestor de archivos y copias
│   └── InstanceGenerator.cs        # Generador principal
├── Program.cs                      # Punto de entrada
├── instances-config.json           # Archivo de configuración
├── build.bat                       # Script de compilación
├── run.bat                         # Script de ejecución
└── README.md                       # Esta documentación
```

## Configuración

### Archivo `instances-config.json`

```json
{
  "sourcePath": "../Pyxoom-Rabbit/bin/Release/net8.0",
  "outputBasePath": "./GeneratedInstances",
  "baseAppSettingsPath": "../Pyxoom-Rabbit/appsettings.json",
  "copyAdditionalFiles": [
    "Content",
    "start-app.bat",
    "start-app.ps1"
  ],
  "excludeFiles": [
    "appsettings.json",
    "appsettings.Development.json"
  ],
  "instances": [
    {
      "instanceName": "Cliente 1 - Producción",
      "folderName": "cliente1-prod",
      "clientId": "1",
      "logPath": "C:/PyxoomLogs/Cliente1",
      "pyxoomInteractiveUrl": "https://cliente1.pyxoomdemo.com/Interactive42/Home/SignInToken",
      "pyxoomInteractivePublicPrivacy": "https://cliente1.pyxoomdemo.com/Interactive42/PublicPrivacy/Index",
      "pyxoomConnectionString": "Data Source=servidor;Database=Pyxoom42_Cliente1;...",
      "externalPyxoomServicesApiUrl": "https://api-cliente1.pyxoomdemo.com/",
      "rabbitMQChannel": "pyxoom_cliente1",
      "empresaId": 1,
      "description": "Instancia de producción para Cliente 1",
      "customSettings": {
        "RabbitMQ:HostName": "rabbit-cliente1.example.com",
        "ExternalServices:NodeAppUrl": "http://node-cliente1:8001/"
      }
    }
  ]
}
```

### Propiedades de Configuración

#### Configuración Principal
- `sourcePath`: Ruta a los archivos compilados de Pyxoom-Rabbit
- `outputBasePath`: Directorio donde se crearán las instancias
- `baseAppSettingsPath`: Ruta al archivo appsettings.json base
- `copyAdditionalFiles`: Archivos adicionales a copiar
- `excludeFiles`: Archivos a excluir durante la copia

#### Configuración de Instancia
- `instanceName`: Nombre descriptivo de la instancia
- `folderName`: Nombre de la carpeta donde se creará la instancia
- `clientId`: ID del cliente (se actualiza en ExternalServices y App)
- `logPath`: Ruta base para los logs de Serilog
- `pyxoomInteractiveUrl`: URL de Interactive42 personalizada
- `pyxoomInteractivePublicPrivacy`: URL de aviso de privacidad
- `pyxoomConnectionString`: Connection string de la base de datos
- `externalPyxoomServicesApiUrl`: URL del API externo
- `rabbitMQChannel`: Canal de RabbitMQ para esta instancia
- `empresaId`: ID de la empresa
- `description`: Descripción de la instancia
- `customSettings`: Configuraciones adicionales en formato "Seccion:Propiedad": "Valor"

## Uso

### A) Ejecución con el EXE compilado (recomendado para soporte)

1) Asegúrate de tener el archivo `instances-config.json` junto al EXE o indica la ruta como argumento.
2) Ejecuta:

```bat
PyxoomInstanceGenerator.exe
```

o con un archivo de configuración específico:

```bat
PyxoomInstanceGenerator.exe C:\ruta\a\instances-config.json
```

El ejecutable creará las carpetas de instancias, generará `appsettings.json` y, al final, escribirá en `outputBasePath`:

- `pm2-commands.txt`
- `pm2-start-all.bat`
- `pm2-start-all.ps1`

Cada línea de estos archivos contiene, por instancia:

```bat
pm2 stop "NombreInstancia"
pm2 delete "NombreInstancia"
pm2 start "C:\ruta\completa\GeneratedInstances\carpeta-instancia\Pyxoom-Rabbit.exe" --name "NombreInstancia" --instances 4
```

Esto garantiza que si el proceso ya existía, se detiene y elimina antes de crearlo de nuevo.

### B) Ejecución desde código fuente (opcional)

```bat
dotnet build PyxoomInstanceGenerator
dotnet run --project PyxoomInstanceGenerator

# O usando los scripts
build.bat
run.bat
```

### Primera ejecución
Si no existe `instances-config.json`, el programa creará automáticamente un archivo de ejemplo para editar.

## Campos Dinámicos Personalizados

El generador actualiza automáticamente estos campos en el `appsettings.json`:

### Campos Principales
- `ExternalServices.ClientId`
- `ExternalServices.PyxoomInteractiveUrl`
- `ExternalServices.PyxoomInteractivePublicPrivacy`
- `App.ClientId`
- `App.EmpresaId`
- `ConnectionStrings.Pyxoom42`
- `ExternalPyxoomServices.ApiUrl`
- `RabbitMQ.Channel`

### Rutas de Logs
- Actualiza automáticamente las rutas en `Serilog.WriteTo` para usar el `logPath` de la instancia
- Cambia el nombre de la aplicación en `Serilog.Properties.Application`

### Configuraciones Personalizadas
Usa `customSettings` para modificar cualquier otra propiedad del `appsettings.json`:

```json
"customSettings": {
  "RabbitMQ:HostName": "servidor-personalizado",
  "ExternalServices:NodeAppUrl": "http://mi-servidor:8001/",
  "Database:ConnectionPoolSize": "20"
}
```

## Resultado

Después de la ejecución, tendrás:

```
GeneratedInstances/
├── cliente1-prod/
│   ├── Pyxoom-Rabbit.exe
│   ├── Pyxoom-Rabbit.dll
│   ├── appsettings.json (personalizado)
│   ├── start-cliente1-prod.bat
│   ├── start-cliente1-prod.ps1
│   └── Content/ (si se especifica en copyAdditionalFiles)
├── cliente2-dev/
│   ├── Pyxoom-Rabbit.exe
│   ├── Pyxoom-Rabbit.dll
│   ├── appsettings.json (personalizado)
│   ├── start-cliente2-dev.bat
│   └── start-cliente2-dev.ps1
└── cliente3-test/
    ├── Pyxoom-Rabbit.exe
    ├── Pyxoom-Rabbit.dll
    ├── appsettings.json (personalizado)
    ├── start-cliente3-test.bat
    └── start-cliente3-test.ps1
```

## Scripts de Inicio

Cada instancia incluye scripts de inicio personalizados:
- `start-[nombre].bat`: Para Windows CMD
- `start-[nombre].ps1`: Para PowerShell

Los scripts muestran la configuración de la instancia y establecen la variable de entorno `QUEUE_NAME` antes de ejecutar la aplicación.

## Ejemplos de Uso

### Configuración para Múltiples Clientes
```json
{
  "instances": [
    {
      "instanceName": "Cliente A - Producción",
      "folderName": "cliente-a-prod",
      "clientId": "100",
      "rabbitMQChannel": "pyxoom_cliente_a_prod",
      "empresaId": 100
    },
    {
      "instanceName": "Cliente B - Desarrollo",
      "folderName": "cliente-b-dev", 
      "clientId": "200",
      "rabbitMQChannel": "pyxoom_cliente_b_dev",
      "empresaId": 200
    }
  ]
}
```

### Configuración para Diferentes Entornos
```json
{
  "instances": [
    {
      "instanceName": "Producción",
      "folderName": "prod",
      "logPath": "C:/Logs/Prod",
      "rabbitMQChannel": "pyxoom_prod"
    },
    {
      "instanceName": "Desarrollo",
      "folderName": "dev",
      "logPath": "C:/Logs/Dev", 
      "rabbitMQChannel": "pyxoom_dev"
    },
    {
      "instanceName": "Testing",
      "folderName": "test",
      "logPath": "C:/Logs/Test",
      "rabbitMQChannel": "pyxoom_test"
    }
  ]
}
```

## Troubleshooting

### Error: "SourcePath no existe"
- Verifica que la ruta `sourcePath` apunte a los archivos compilados de Pyxoom-Rabbit
- Asegúrate de que los archivos `Pyxoom-Rabbit.exe` y `Pyxoom-Rabbit.dll` estén presentes

### Error: "BaseAppSettingsPath no existe"
- Verifica que la ruta `baseAppSettingsPath` apunte al archivo `appsettings.json` original
- El archivo debe ser un JSON válido

### Error: "FolderName duplicado"
- Cada instancia debe tener un `folderName` único
- Verifica que no haya duplicados en el array `instances`

### Logs no se generan en la ubicación esperada
- Verifica que el directorio especificado en `logPath` existe y tiene permisos de escritura
- El generador solo actualiza las rutas en la configuración, no crea los directorios

## Requisitos

- .NET 8.0 SDK
- Archivos compilados de Pyxoom-Rabbit
- Archivo `appsettings.json` base de Pyxoom-Rabbit

## PM2: Operación y Mantenimiento (Soporte)

Requisitos previos:
- Node.js y PM2 instalados en el servidor.
- Se recomienda ejecutar PM2 en el contexto adecuado (usuario/servicio) según tu operación.

Archivos generados por este generador (en `outputBasePath`):
- `pm2-commands.txt`: comandos legibles por instancia
- `pm2-start-all.bat`: ejecuta stop/delete/start para todas las instancias
- `pm2-start-all.ps1`: equivalente para PowerShell

Ejecución típica (Windows CMD):
```bat
pm2-start-all.bat
```

Esto intentará detener y eliminar cada proceso por nombre y luego iniciarlo nuevamente con 4 instancias.

### Detener/Eliminar una instancia específica (sin afectar otras)

Usa exactamente el nombre configurado (`InstanceName` en `instances-config.json`):

```bat
pm2 stop "NombreInstancia"
pm2 delete "NombreInstancia"
```

Para ver los procesos activos:
```bat
pm2 list
```

### Detener/Eliminar solo las instancias generadas por este archivo (todas a la vez)

Método seguro: usa los nombres presentes en `pm2-commands.txt`.

- Windows CMD (detener todas):
  1) Abre `pm2-commands.txt`
  2) Copia únicamente las líneas `pm2 stop "..."` y pégalas en la terminal

- Windows CMD (eliminar todas):
  1) Abre `pm2-commands.txt`
  2) Copia únicamente las líneas `pm2 delete "..."` y pégalas en la terminal

Así evitas impactar otros servicios PM2 que no fueron generados aquí.

### Actualizar un ambiente existente

1) Vuelve a ejecutar `PyxoomInstanceGenerator.exe` (con el `instances-config.json` actualizado). 
2) Ejecuta `pm2-start-all.bat` para recrear los procesos. El BAT ya hace stop/delete antes de start, por lo que actualiza sin duplicados.

### Notas
- Las rutas usadas en los comandos PM2 son absolutas y normalizadas, lo cual evita problemas de separadores en Windows.
- El nombre PM2 de cada proceso es exactamente el `InstanceName` del `instances-config.json`. Cambiar ese nombre implica crear un nuevo proceso con nombre distinto en PM2.

## Licencia

Este proyecto es parte del ecosistema Pyxoom-Rabbit.
