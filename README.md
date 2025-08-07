# 🎨 PixelTone

**PixelTone** es una aplicación de escritorio desarrollada en **C# con WPF (.NET 8)** que permite aplicar filtros visuales tanto a imágenes como a videos, detectar colores específicos en tiempo real a través de la cámara, y generar histogramas detallados de cada proceso. Ideal para proyectos de procesamiento digital de imágenes.

---

## 🧠 Características principales

### 📷 Procesamiento de Imágenes
- Carga de imágenes desde el disco.
- Aplicación de filtros personalizados (blanco y negro, sepia, negativo, etc).
- Generación de histograma de colores por canal (R, G, B).

### 🎞️ Procesamiento de Videos
- Carga de videos y aplicación de filtros cuadro por cuadro.
- Visualización del video con filtro aplicado en tiempo real.
- Histograma dinámico del video procesado.

### 🔍 Detección en tiempo real (cámara)
- Captura de video desde cámara web.
- Detección de colores específicos definidos por el usuario.
- Segmentación visual del color detectado.
- Histograma en tiempo real durante la detección.

---

## 🛠️ Tecnologías usadas

- **Lenguaje:** C#
- **Framework:** WPF (.NET 8)

---

## 🚀 Cómo ejecutar el proyecto

1. Clona este repositorio:

   ```bash
   git clone https://github.com/JoseDelacruzS/PixelTone.git
2. Abre la solución en Visual Studio 2022 o superior.

3. Restaura los paquetes NuGet si es necesario.

4. Ejecuta el proyecto (F5) y explora las funciones.

--- 

## 📁 Estructura del proyecto

PixelTone/
│
├── Assets/                  # Imágenes de prueba y recursos visuales
├── Modules/
│   ├── ImageFilters/        # Lógica para filtros de imágenes
│   ├── VideoFilters/        # Lógica para filtros de video
│   └── ColorDetection/      # Detección de colores con cámara
│
├── Histograms/              # Generación de histogramas
├── Views/                   # Interfaces WPF
├── App.xaml                 # Configuración general de la app
└── MainWindow.xaml          # Ventana principal

---

## 📌 Funcionalidades pendientes / Por mejorar
 Exportar imágenes y videos procesados

 Guardar histogramas como imagen o PDF

 Ajustes dinámicos de los filtros por usuario

 Detección múltiple de colores en tiempo real

---

##🧑‍💻 Autor
José de la Cruz (Chucho)
Líder de desarrollo y programación de PixelTone
