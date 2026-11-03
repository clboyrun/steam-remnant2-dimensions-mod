from pathlib import Path
import sys

if __name__ == "__main__":
    source_root = Path(__file__).resolve().parent
    builder_root = source_root.parent
    sys.path.insert(0, str(source_root))

    config_path = builder_root / "config" / "pipeline.json"
    if "--run" in sys.argv:
        from pipeline import Pipeline

        Pipeline(config_path).execute()
    else:
        from ui.main_window import MainWindow

        app = MainWindow(config_path)
        app.mainloop()