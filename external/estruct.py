import sys
import json
from pathlib import Path
from PyQt5.QtWidgets import (
    QApplication, QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QLabel,
    QFileDialog, QListWidget, QMessageBox, QCheckBox, QLineEdit
)

# Lista de extensiones de archivo a incluir en la combinación.
EXTENSIONES = [
    ".cs", ".xaml", ".js", ".jsx", ".html", ".css", 
    ".axaml", ".axaml.cs", ".sln", ".csproj", ".json",
    ".ts", ".tsx", ".py", ".md", ".xaml.cs"
]

# --- NUEVO: Ubicación del archivo de configuración ---
# Se guardará en una carpeta oculta en el directorio de inicio del usuario
# para no interferir con otros archivos.
CONFIG_DIR = Path.home() / ".file_combiner_config"
CONFIG_FILE = CONFIG_DIR / "settings.json"


class FileCombiner(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Combinador de Archivos de Proyecto con Filtros")
        self.setGeometry(100, 100, 700, 600)

        self.ruta_proyecto_raiz = None
        self.archivos_encontrados = []
        
        # --- NUEVO: Cargar la configuración al iniciar ---
        self.carpetas_excluidas = []
        self.cargar_configuracion()

        # --- Configuración de la Interfaz Gráfica (UI) ---
        self.layout = QVBoxLayout()

        # Sección Principal
        self.btn_seleccionar_carpeta = QPushButton("1. Seleccionar Carpeta Raíz del Proyecto")
        self.btn_seleccionar_carpeta.clicked.connect(self.seleccionar_carpeta_proyecto)
        
        # --- NUEVO: Sección para gestionar exclusiones ---
        self.setup_exclusions_ui()

        # Lista de archivos encontrados
        self.layout.addWidget(QLabel("Archivos que se incluirán:"))
        self.lista_archivos = QListWidget()
        
        # Sección de acciones
        self.checkbox_separar = QCheckBox("Crear archivos de salida separados por cada subcarpeta")
        self.btn_combinar = QPushButton("3. Combinar y Guardar Archivos")
        self.btn_combinar.clicked.connect(self.combinar_archivos)

        # Añadir widgets al layout principal
        self.layout.addWidget(self.btn_seleccionar_carpeta)
        self.layout.addWidget(self.lista_archivos)
        self.layout.addWidget(self.checkbox_separar)
        self.layout.addWidget(self.btn_combinar)

        self.setLayout(self.layout)

    def setup_exclusions_ui(self):
        """Crea y añade los widgets para la gestión de exclusiones."""
        group_label = QLabel("2. Carpetas a Excluir (los cambios se guardan automáticamente)")
        group_label.setStyleSheet("font-weight: bold;")
        self.layout.addWidget(group_label)

        self.lista_exclusiones = QListWidget()
        self.lista_exclusiones.setFixedHeight(100) # Tamaño fijo para que no ocupe toda la pantalla
        self.actualizar_lista_exclusiones_ui()

        # Layout horizontal para los botones de añadir/quitar
        hbox = QHBoxLayout()
        self.input_exclusion = QLineEdit()
        self.input_exclusion.setPlaceholderText("Nombre de la carpeta a excluir (ej: node_modules)")
        self.btn_agregar_exclusion = QPushButton("Añadir")
        self.btn_quitar_exclusion = QPushButton("Quitar Seleccionada")

        self.btn_agregar_exclusion.clicked.connect(self.agregar_exclusion)
        self.btn_quitar_exclusion.clicked.connect(self.quitar_exclusion)
        
        hbox.addWidget(self.input_exclusion)
        hbox.addWidget(self.btn_agregar_exclusion)
        hbox.addWidget(self.btn_quitar_exclusion)
        
        self.layout.addWidget(self.lista_exclusiones)
        self.layout.addLayout(hbox)

    # --- NUEVO: Métodos para gestionar la configuración ---

    def cargar_configuracion(self):
        """Carga la lista de exclusiones desde el archivo JSON."""
        try:
            if CONFIG_FILE.exists():
                with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    # Se asegura de que sean strings y no haya duplicados
                    self.carpetas_excluidas = sorted(list(set(config.get("carpetas_excluidas", []))))
            else:
                # Si no existe el archivo, crea una lista con valores por defecto
                self.carpetas_excluidas = ['.git', 'node_modules', 'bin', 'obj', '.vscode', '__pycache__']
                self.guardar_configuracion()
        except (json.JSONDecodeError, IOError) as e:
            print(f"Error al cargar configuración, usando valores por defecto: {e}")
            self.carpetas_excluidas = ['.git', 'node_modules', 'bin', 'obj', '.vscode', '__pycache__']

    def guardar_configuracion(self):
        """Guarda la lista actual de exclusiones en el archivo JSON."""
        try:
            # Asegura que el directorio de configuración exista
            CONFIG_DIR.mkdir(parents=True, exist_ok=True)
            with open(CONFIG_FILE, 'w', encoding='utf-8') as f:
                config = {"carpetas_excluidas": self.carpetas_excluidas}
                json.dump(config, f, indent=4)
        except IOError as e:
            QMessageBox.critical(self, "Error de Guardado", f"No se pudo guardar la configuración:\n{e}")

    def agregar_exclusion(self):
        """Añade una nueva carpeta a la lista de exclusiones."""
        carpeta = self.input_exclusion.text().strip()
        if carpeta and carpeta not in self.carpetas_excluidas:
            self.carpetas_excluidas.append(carpeta)
            self.carpetas_excluidas.sort()
            self.guardar_configuracion()
            self.actualizar_lista_exclusiones_ui()
            self.input_exclusion.clear()
            # Si ya hay un proyecto cargado, refresca la lista de archivos
            if self.ruta_proyecto_raiz:
                self.actualizar_lista_de_archivos()

    def quitar_exclusion(self):
        """Quita la carpeta seleccionada de la lista de exclusiones."""
        item_seleccionado = self.lista_exclusiones.currentItem()
        if item_seleccionado:
            carpeta = item_seleccionado.text()
            if carpeta in self.carpetas_excluidas:
                self.carpetas_excluidas.remove(carpeta)
                self.guardar_configuracion()
                self.actualizar_lista_exclusiones_ui()
                # Si ya hay un proyecto cargado, refresca la lista de archivos
                if self.ruta_proyecto_raiz:
                    self.actualizar_lista_de_archivos()

    def actualizar_lista_exclusiones_ui(self):
        """Refresca el QListWidget con la lista actual de exclusiones."""
        self.lista_exclusiones.clear()
        self.lista_exclusiones.addItems(self.carpetas_excluidas)

    # --- Métodos de la lógica principal (modificados) ---
    
    def seleccionar_carpeta_proyecto(self):
        ruta = QFileDialog.getExistingDirectory(self, "Seleccionar la carpeta raíz del proyecto")
        if not ruta:
            return

        self.ruta_proyecto_raiz = Path(ruta)
        self.actualizar_lista_de_archivos()

    def actualizar_lista_de_archivos(self):
        self.archivos_encontrados.clear()
        self.lista_archivos.clear()

        if not self.ruta_proyecto_raiz:
            return

        self.lista_archivos.addItem(f"[PROYECTO] -> {self.ruta_proyecto_raiz}")

        for archivo in sorted(self.ruta_proyecto_raiz.rglob("*")):
            if not archivo.is_file():
                continue

            # --- LÓGICA DE FILTRADO MEJORADA ---
            # Convierte la ruta en una tupla de sus partes (carpetas y archivo)
            partes_ruta = archivo.relative_to(self.ruta_proyecto_raiz).parts
            # Comprueba si alguna de las carpetas excluidas está en la ruta del archivo
            if any(carpeta_excluida in partes_ruta for carpeta_excluida in self.carpetas_excluidas):
                continue

            if archivo.suffix.lower() in EXTENSIONES:
                self.archivos_encontrados.append(archivo)
                ruta_relativa = archivo.relative_to(self.ruta_proyecto_raiz)
                self.lista_archivos.addItem(f"  + {ruta_relativa}")
        
        if not self.archivos_encontrados:
            QMessageBox.warning(self, "Sin archivos", "No se encontraron archivos válidos (después de aplicar los filtros) en la carpeta seleccionada.")

    def combinar_archivos(self):
        if not self.archivos_encontrados:
            QMessageBox.warning(self, "Advertencia", "No hay archivos para combinar.")
            return

        directorio_salida = QFileDialog.getExistingDirectory(self, "Seleccionar dónde guardar el/los archivo(s) de salida")
        if not directorio_salida:
            return

        try:
            if self.checkbox_separar.isChecked():
                archivos_por_carpeta = {}
                for archivo in self.archivos_encontrados:
                    nombre_carpeta = archivo.parent.name
                    archivos_por_carpeta.setdefault(nombre_carpeta, []).append(archivo)

                for nombre_carpeta, lista_archivos in archivos_por_carpeta.items():
                    ruta_salida = Path(directorio_salida) / f"Combined_{self.ruta_proyecto_raiz.name}_{nombre_carpeta}.txt"
                    self.escribir_archivos_en_salida(ruta_salida, lista_archivos)
            else:
                ruta_salida = Path(directorio_salida) / f"Combined_{self.ruta_proyecto_raiz.name}_All.txt"
                self.escribir_archivos_en_salida(ruta_salida, self.archivos_encontrados)

            QMessageBox.information(self, "Éxito", f"Archivos combinados correctamente en:\n{directorio_salida}")
        except Exception as e:
            QMessageBox.critical(self, "Error", f"Ocurrió un error al combinar los archivos:\n{e}")

    def escribir_archivos_en_salida(self, ruta_salida, lista_de_archivos):
        with ruta_salida.open("w", encoding="utf-8", errors="ignore") as f_out:
            for archivo in lista_de_archivos:
                try:
                    ruta_relativa = archivo.relative_to(self.ruta_proyecto_raiz)
                    contenido = archivo.read_text(encoding="utf-8", errors="ignore")
                    
                    f_out.write(f"//-{'='*20} START OF FILE: {ruta_relativa} {'='*20}-//\n\n")
                    f_out.write(contenido)
                    f_out.write(f"\n\n//-{'='*20} END OF FILE: {ruta_relativa} {'='*20}-//\n\n")
                except Exception as e:
                    f_out.write(f"//-!!! ERROR READING FILE: {ruta_relativa} - {e} !!!-//\n\n")


if __name__ == "__main__":
    app = QApplication(sys.argv)
    ventana = FileCombiner()
    ventana.show()
    sys.exit(app.exec_())