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
    ".ts", ".tsx", ".py", ".md", ".xaml.cs", ".rs", ".json"
]

# Ubicación del archivo de configuración
CONFIG_DIR = Path.home() / ".file_combiner_config"
CONFIG_FILE = CONFIG_DIR / "settings.json"


class FileCombiner(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Combinador de Archivos de Proyecto con Filtros")
        self.setGeometry(100, 100, 700, 800) # Aumentamos un poco la altura para la nueva sección

        self.ruta_proyecto_raiz = None
        self.archivos_encontrados = []
        
        # --- MODIFICADO: Ahora también manejamos archivos excluidos ---
        self.carpetas_excluidas = []
        self.archivos_excluidos = [] # --- NUEVO ---
        self.cargar_configuracion()

        # --- Configuración de la Interfaz Gráfica (UI) ---
        self.layout = QVBoxLayout()

        # Sección Principal
        self.btn_seleccionar_carpeta = QPushButton("1. Seleccionar Carpeta Raíz del Proyecto")
        self.btn_seleccionar_carpeta.clicked.connect(self.seleccionar_carpeta_proyecto)
        self.layout.addWidget(self.btn_seleccionar_carpeta)

        # --- GESTIÓN DE EXCLUSIONES ---
        self.setup_folder_exclusions_ui() # Exclusión de carpetas
        self.setup_file_exclusions_ui()   # --- NUEVO: Exclusión de archivos ---

        # Lista de archivos encontrados
        self.layout.addWidget(QLabel("Archivos que se incluirán:"))
        self.lista_archivos = QListWidget()
        self.layout.addWidget(self.lista_archivos)
        
        # Sección de acciones
        self.checkbox_separar = QCheckBox("Crear archivos de salida separados por cada subcarpeta")
        self.btn_combinar = QPushButton("4. Combinar y Guardar Archivos")
        self.btn_combinar.clicked.connect(self.combinar_archivos)

        self.layout.addWidget(self.checkbox_separar)
        self.layout.addWidget(self.btn_combinar)

        self.setLayout(self.layout)

    def setup_folder_exclusions_ui(self):
        """Crea y añade los widgets para la gestión de exclusiones de CARPETAS."""
        group_label = QLabel("2. Carpetas a Excluir (los cambios se guardan automáticamente)")
        group_label.setStyleSheet("font-weight: bold;")
        self.layout.addWidget(group_label)

        self.lista_exclusiones_carpetas = QListWidget()
        self.lista_exclusiones_carpetas.setFixedHeight(100)
        self.actualizar_lista_exclusiones_carpetas_ui()

        hbox = QHBoxLayout()
        self.input_exclusion_carpeta = QLineEdit()
        self.input_exclusion_carpeta.setPlaceholderText("Nombre de la carpeta (ej: node_modules)")
        self.btn_agregar_exclusion_carpeta = QPushButton("Añadir")
        self.btn_quitar_exclusion_carpeta = QPushButton("Quitar")

        self.btn_agregar_exclusion_carpeta.clicked.connect(self.agregar_exclusion_carpeta)
        self.btn_quitar_exclusion_carpeta.clicked.connect(self.quitar_exclusion_carpeta)
        
        hbox.addWidget(self.input_exclusion_carpeta)
        hbox.addWidget(self.btn_agregar_exclusion_carpeta)
        hbox.addWidget(self.btn_quitar_exclusion_carpeta)
        
        self.layout.addWidget(self.lista_exclusiones_carpetas)
        self.layout.addLayout(hbox)
    
    # --- NUEVO: UI para exclusión de archivos ---
    def setup_file_exclusions_ui(self):
        """Crea y añade los widgets para la gestión de exclusiones de ARCHIVOS."""
        group_label = QLabel("3. Archivos a Excluir (por nombre exacto)")
        group_label.setStyleSheet("font-weight: bold;")
        self.layout.addWidget(group_label)

        self.lista_exclusiones_archivos = QListWidget()
        self.lista_exclusiones_archivos.setFixedHeight(100)
        self.actualizar_lista_exclusiones_archivos_ui()

        hbox = QHBoxLayout()
        self.input_exclusion_archivo = QLineEdit()
        self.input_exclusion_archivo.setPlaceholderText("Nombre del archivo (ej: Cargo.lock)")
        self.btn_agregar_exclusion_archivo = QPushButton("Añadir")
        self.btn_quitar_exclusion_archivo = QPushButton("Quitar")

        self.btn_agregar_exclusion_archivo.clicked.connect(self.agregar_exclusion_archivo)
        self.btn_quitar_exclusion_archivo.clicked.connect(self.quitar_exclusion_archivo)

        hbox.addWidget(self.input_exclusion_archivo)
        hbox.addWidget(self.btn_agregar_exclusion_archivo)
        hbox.addWidget(self.btn_quitar_exclusion_archivo)

        self.layout.addWidget(self.lista_exclusiones_archivos)
        self.layout.addLayout(hbox)

    # --- MODIFICADO: Métodos de configuración para carpetas y archivos ---

    def cargar_configuracion(self):
        """Carga las listas de exclusiones desde el archivo JSON."""
        try:
            if CONFIG_FILE.exists():
                with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    self.carpetas_excluidas = sorted(list(set(config.get("carpetas_excluidas", []))))
                    self.archivos_excluidos = sorted(list(set(config.get("archivos_excluidos", [])))) # --- NUEVO ---
            else:
                self.carpetas_excluidas = ['.git', 'node_modules', 'bin', 'obj', '.vscode', '__pycache__', 'target']
                self.archivos_excluidos = ['package-lock.json', 'Cargo.lock'] # --- NUEVO: Valores por defecto ---
                self.guardar_configuracion()
        except (json.JSONDecodeError, IOError) as e:
            print(f"Error al cargar configuración, usando valores por defecto: {e}")
            self.carpetas_excluidas = ['.git', 'node_modules', 'bin', 'obj', '.vscode', '__pycache__', 'target']
            self.archivos_excluidos = ['package-lock.json', 'Cargo.lock'] # --- NUEVO ---

    def guardar_configuracion(self):
        """Guarda las listas de exclusiones en el archivo JSON."""
        try:
            CONFIG_DIR.mkdir(parents=True, exist_ok=True)
            with open(CONFIG_FILE, 'w', encoding='utf-8') as f:
                config = {
                    "carpetas_excluidas": self.carpetas_excluidas,
                    "archivos_excluidos": self.archivos_excluidos # --- NUEVO ---
                }
                json.dump(config, f, indent=4)
        except IOError as e:
            QMessageBox.critical(self, "Error de Guardado", f"No se pudo guardar la configuración:\n{e}")

    # --- Métodos para gestión de CARPETAS ---
    def agregar_exclusion_carpeta(self):
        carpeta = self.input_exclusion_carpeta.text().strip()
        if carpeta and carpeta not in self.carpetas_excluidas:
            self.carpetas_excluidas.append(carpeta)
            self.carpetas_excluidas.sort()
            self.guardar_configuracion()
            self.actualizar_lista_exclusiones_carpetas_ui()
            self.input_exclusion_carpeta.clear()
            if self.ruta_proyecto_raiz:
                self.actualizar_lista_de_archivos()

    def quitar_exclusion_carpeta(self):
        item_seleccionado = self.lista_exclusiones_carpetas.currentItem()
        if item_seleccionado:
            carpeta = item_seleccionado.text()
            if carpeta in self.carpetas_excluidas:
                self.carpetas_excluidas.remove(carpeta)
                self.guardar_configuracion()
                self.actualizar_lista_exclusiones_carpetas_ui()
                if self.ruta_proyecto_raiz:
                    self.actualizar_lista_de_archivos()

    def actualizar_lista_exclusiones_carpetas_ui(self):
        self.lista_exclusiones_carpetas.clear()
        self.lista_exclusiones_carpetas.addItems(self.carpetas_excluidas)

    # --- NUEVO: Métodos para gestión de ARCHIVOS ---
    def agregar_exclusion_archivo(self):
        archivo = self.input_exclusion_archivo.text().strip()
        if archivo and archivo not in self.archivos_excluidos:
            self.archivos_excluidos.append(archivo)
            self.archivos_excluidos.sort()
            self.guardar_configuracion()
            self.actualizar_lista_exclusiones_archivos_ui()
            self.input_exclusion_archivo.clear()
            if self.ruta_proyecto_raiz:
                self.actualizar_lista_de_archivos()

    def quitar_exclusion_archivo(self):
        item_seleccionado = self.lista_exclusiones_archivos.currentItem()
        if item_seleccionado:
            archivo = item_seleccionado.text()
            if archivo in self.archivos_excluidos:
                self.archivos_excluidos.remove(archivo)
                self.guardar_configuracion()
                self.actualizar_lista_exclusiones_archivos_ui()
                if self.ruta_proyecto_raiz:
                    self.actualizar_lista_de_archivos()

    def actualizar_lista_exclusiones_archivos_ui(self):
        self.lista_exclusiones_archivos.clear()
        self.lista_exclusiones_archivos.addItems(self.archivos_excluidos)
    
    # --- Lógica principal ---
    
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

            # --- LÓGICA DE FILTRADO MODIFICADA ---
            # 1. Excluir por CARPETA
            partes_ruta = archivo.relative_to(self.ruta_proyecto_raiz).parts
            if any(carpeta_excluida in partes_ruta for carpeta_excluida in self.carpetas_excluidas):
                continue

            # 2. --- NUEVO: Excluir por NOMBRE DE ARCHIVO exacto ---
            if archivo.name in self.archivos_excluidos:
                continue

            # 3. Incluir por EXTENSIÓN (si pasa los filtros anteriores)
            # Excepción: no excluir package.json si .json está en las extensiones permitidas
            if archivo.name == 'package.json' and '.json' in EXTENSIONES and 'package.json' not in self.archivos_excluidos:
                 pass # Permite que package.json se incluya si no está explícitamente excluido
            
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