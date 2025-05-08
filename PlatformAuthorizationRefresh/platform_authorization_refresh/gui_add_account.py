"""
Presence - Social Media Account Authorization
------------------------------------------------
This application provides a graphical interface for authorizing social media accounts (YouTube and TikTok)
via an external refresh executable. It uses Tkinter for the GUI and employs subprocesses and threading to run
the executable and stream its output in real time. ANSI escape sequences from the process output are parsed and
displayed in the GUI with color styling; the log file output will be clean (ANSI codes stripped).

Usage:
    python gui_add_account.exe --settings <path_to_appsettings.json> [--log <path_to_log_file>]

If the --settings argument is not provided, the script defaults to "appsettings.json" in the same folder.
If the --log argument is not provided, a "Logs" folder is created and a log file named
"presence_add_account_log_<timestamp>.txt" is used.

Author: Your Name
Date: YYYY-MM-DD
"""

import argparse
import tkinter as tk
from tkinter import ttk, filedialog
import subprocess
import sys
import json
import threading
import queue
import time
import re
from pathlib import Path
import logging
from typing import Optional, List, Tuple, Any

###############################################################################
# LOG FILE SETUP AND CUSTOM FORMATTER (STRIPS ANSI CODES FOR THE FILE OUTPUT)
###############################################################################
def get_log_file_path(provided_log: Optional[Path]) -> Path:
    if provided_log is not None:
        return provided_log
    else:
        logs_dir = Path(__file__).parent / "Logs"
        logs_dir.mkdir(exist_ok=True)
        timestamp = time.strftime("%Y%m%d_%H%M%S")
        return logs_dir / f"presence_add_account_log_{timestamp}.txt"

class AnsiStripFormatter(logging.Formatter):
    ansi_escape = re.compile(r'\x1b\[[0-9;]*m')
    def format(self, record):
        s = super().format(record)
        return self.ansi_escape.sub('', s)

###############################################################################
# COMMAND-LINE ARGUMENT PARSING
###############################################################################
parser = argparse.ArgumentParser(
    description="Authorize social media accounts using Presence."
)
parser.add_argument("appsettings", type=Path,
                    help="Path to the appsettings.json file. This argument is required.")
parser.add_argument("--log", dest="log_file", type=Path, default=None,
                    help="Optional log file path. If not provided, a 'Logs' folder with a timestamped filename is used.")

args = parser.parse_args()
log_file_path = get_log_file_path(args.log_file)

###############################################################################
# CONFIGURE LOGGING: Console retains ANSI codes, file output is stripped.
###############################################################################
console_handler = logging.StreamHandler()
file_handler = logging.FileHandler(filename=str(log_file_path), encoding="utf-8")
formatter = logging.Formatter("%(asctime)s [%(levelname)s] %(message)s")
ansi_formatter = AnsiStripFormatter("%(asctime)s [%(levelname)s] %(message)s")
console_handler.setFormatter(formatter)
file_handler.setFormatter(ansi_formatter)
logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
logger.addHandler(console_handler)
logger.addHandler(file_handler)

###############################################################################
# THEME CONSTANTS
###############################################################################
COLOR_BACKGROUND = "#f6f5fa"
COLOR_FRAME_BG = "#ffffff"
COLOR_YOUTUBE = "#4285F4"
COLOR_TIKTOK = "#FF0050"
COLOR_SUCCESS = "#34A853"
COLOR_ERROR = "#EA4335"
COLOR_PRESENCE = "#9370DB"
COLOR_PRESENCE_DARK = "#7851A9"

###############################################################################
# MAIN APPLICATION CLASS
###############################################################################
class PresenceAuthApp:
    def __init__(self, appsettings: dict) -> None:
        """
        Initialize the PresenceAuthApp with configuration settings.

        :param appsettings: Dictionary containing configuration parameters.
        """
        self.appsettings = appsettings
        self.credentials_folder_base: str = appsettings.get("CredentialsFolderBasePath", "")
        self.creds_youtube_file: str = appsettings.get("CredentialsYoutubeFileName", "YouTubeCredentials.json")
        self.creds_tiktok_file: str = appsettings.get("CredentialsTikTokFileName", "TikTokCredentials.json")
        self.authorization_refresh_exe_path: str = appsettings.get("AuthorizationRefreshExePath", "")
        self.authorization_refresh_chrome_path: str = appsettings.get("AuthorizationRefreshChromePath", "")
        self.default_creds_path: str = str(Path(self.credentials_folder_base) / self.creds_youtube_file)
        self.chrome_path_cache: Optional[str] = None
        self.current_process: Optional[subprocess.Popen] = None
        self.setup_ui()

    def setup_ui(self) -> None:
        """Initialize and configure the Tkinter GUI."""
        self.root = tk.Tk()
        self.root.title("Presence - Social Media Account Authorization")
        self.root.geometry("800x610")
        self.root.configure(bg=COLOR_BACKGROUND)

        logger.info("GUI opened.")

        self.presence_logo: Optional[tk.PhotoImage] = self.load_presence_logo()

        self.style = ttk.Style()
        self.style.theme_use('clam')
        self.style.configure('TFrame', background=COLOR_BACKGROUND)
        self.style.configure('Card.TFrame', background=COLOR_FRAME_BG)
        self.style.configure('TLabel', background=COLOR_FRAME_BG, foreground="#202124", font=('Segoe UI', 9))
        self.style.configure('Header.TLabel', font=('Segoe UI', 14, 'bold'),
                             foreground=COLOR_PRESENCE_DARK, background=COLOR_FRAME_BG)
        self.style.configure('TEntry', font=('Segoe UI', 9), fieldbackground="white")
        self.style.configure('TButton', font=('Segoe UI', 9))
        self.style.configure('TCheckbutton', background=COLOR_FRAME_BG, font=('Segoe UI', 9))
        self.style.configure('NormalStatus.TLabel', background=COLOR_BACKGROUND, foreground="#5f6368")
        self.style.configure('ErrorStatus.TLabel', background=COLOR_BACKGROUND, foreground=COLOR_ERROR)
        self.style.configure('SuccessStatus.TLabel', background=COLOR_BACKGROUND, foreground=COLOR_SUCCESS)
        self.style.configure('Accent.TButton', background=COLOR_PRESENCE, foreground="white")

        self.main_frame = ttk.Frame(self.root, style='TFrame')
        self.main_frame.pack(fill=tk.BOTH, expand=True, padx=15, pady=15)

        self.setup_header_frame()
        self.setup_form_frame()
        self.setup_buttons_frame()
        self.setup_output_frame()

        self.root.update_idletasks()
        width = self.root.winfo_width()
        height = self.root.winfo_height()
        x = (self.root.winfo_screenwidth() // 2) - (width // 2)
        y = 20
        self.root.geometry(f"{width}x{height}+{x}+{y}")

    def load_presence_logo(self) -> Optional[tk.PhotoImage]:
        """
        Load the Presence logo.

        :return: A PhotoImage instance or None.
        """
        try:
            logo_path = Path("img/Presence LOGO v0.png")
            if not logo_path.exists():
                logger.info("Logo file does not exist: %s", logo_path)
                return None
            return tk.PhotoImage(file=str(logo_path))
        except Exception as e:
            logger.info("Error loading logo: %s", e)
            return None

    def setup_header_frame(self) -> None:
        """Create the header frame with the logo and title."""
        self.header_frame = ttk.Frame(self.main_frame, style='Card.TFrame')
        self.header_frame.pack(fill=tk.X, pady=(0, 10))
        self.header_frame['borderwidth'] = 1
        self.header_frame['relief'] = 'solid'

        self.platform_indicator = tk.Frame(self.header_frame, width=8, background=COLOR_PRESENCE)
        self.platform_indicator.pack(side=tk.LEFT, fill=tk.Y)

        title_frame = ttk.Frame(self.header_frame, style='Card.TFrame')
        title_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=15, pady=5)

        if self.presence_logo:
            try:
                logo_size = self.presence_logo.subsample(int(self.presence_logo.width() / 65))
                logo_label = tk.Label(title_frame, image=logo_size, background=COLOR_FRAME_BG)
                logo_label.image = logo_size
                logo_label.pack(side=tk.LEFT, padx=(0, 10))
            except Exception as e:
                logger.info("Error displaying logo: %s", e)
                fallback_logo = tk.Frame(title_frame, width=40, height=40, background=COLOR_PRESENCE)
                fallback_logo.pack(side=tk.LEFT, padx=(0, 10))
        else:
            fallback_logo = tk.Frame(title_frame, width=40, height=40, background=COLOR_PRESENCE)
            fallback_logo.pack(side=tk.LEFT, padx=(0, 10))

        title_label = ttk.Label(title_frame, text="Add a new account to Presence!", style='Header.TLabel')
        title_label.pack(anchor='w')
        subtitle_label = ttk.Label(title_frame, text="Securely authorize and add a new account to the system",
                                   foreground="#5f6368", background=COLOR_FRAME_BG)
        subtitle_label.pack(anchor='w', pady=(2, 0))

    def setup_form_frame(self) -> None:
        """Create the form frame for user input."""
        self.form_frame = ttk.Frame(self.main_frame, style='Card.TFrame')
        self.form_frame.pack(fill=tk.X, pady=(0, 10))
        self.form_frame['borderwidth'] = 1
        self.form_frame['relief'] = 'solid'

        self.form_inner = ttk.Frame(self.form_frame, style='Card.TFrame')
        self.form_inner.pack(fill=tk.X, padx=15, pady=5)
        self.form_inner.columnconfigure(0, weight=0, minsize=110)
        self.form_inner.columnconfigure(1, weight=1)
        self.form_inner.columnconfigure(2, weight=0)

        ttk.Label(self.form_inner, text="Account Email:", style='TLabel')\
            .grid(row=0, column=0, sticky='w', padx=5, pady=5)
        entry_frame = ttk.Frame(self.form_inner, style='Card.TFrame')
        entry_frame.grid(row=0, column=1, columnspan=2, sticky='ew', padx=5, pady=5)
        self.entry_account = ttk.Entry(entry_frame, font=('Segoe UI', 9), width=40)
        self.entry_account.pack(fill=tk.X)

        ttk.Label(self.form_inner, text="Platform:", style='TLabel')\
            .grid(row=1, column=0, sticky='w', padx=5, pady=5)
        platform_frame = ttk.Frame(self.form_inner, style='Card.TFrame')
        platform_frame.grid(row=1, column=1, columnspan=2, sticky='ew', padx=5, pady=5)
        self.combo_platform = ttk.Combobox(platform_frame, values=["YouTube", "TikTok"],
                                           state="normal", font=('Segoe UI', 9), width=38)
        self.combo_platform.current(0)
        self.combo_platform.pack(fill=tk.X)
        self.combo_platform.bind("<<ComboboxSelected>>", self.update_creds_path)

        ttk.Label(self.form_inner, text="Credentials File:", style='TLabel')\
            .grid(row=2, column=0, sticky='w', padx=5, pady=5)
        self.entry_creds = ttk.Entry(self.form_inner, font=('Segoe UI', 9))
        self.entry_creds.grid(row=2, column=1, sticky='ew', padx=5, pady=5)
        self.entry_creds.insert(0, self.default_creds_path)
        self.browse_creds_btn = ttk.Button(self.form_inner, text="...", width=2, command=self.browse_creds_file)
        self.browse_creds_btn.grid(row=2, column=2, sticky='e', padx=5, pady=5)

        ttk.Label(self.form_inner, text="Chrome Path:", style='TLabel')\
            .grid(row=3, column=0, sticky='w', padx=5, pady=5)
        self.entry_chrome = ttk.Entry(self.form_inner, font=('Segoe UI', 9))
        self.entry_chrome.grid(row=3, column=1, sticky='ew', padx=5, pady=5)
        if self.authorization_refresh_chrome_path:
            self.entry_chrome.insert(0, self.authorization_refresh_chrome_path)
        self.browse_chrome_btn = ttk.Button(self.form_inner, text="...", width=2, command=self.browse_chrome_path)
        self.browse_chrome_btn.grid(row=3, column=2, sticky='e', padx=5, pady=5)

        self.use_default_browser_var = tk.BooleanVar(value=False)
        default_browser_frame = ttk.Frame(self.form_inner, style='Card.TFrame')
        default_browser_frame.grid(row=4, column=0, columnspan=3, sticky='w', padx=5, pady=(0, 5))
        self.default_browser_cb = ttk.Checkbutton(default_browser_frame,
                                                  text="Use default browser instead of Chrome path",
                                                  variable=self.use_default_browser_var,
                                                  command=self.toggle_default_browser)
        self.default_browser_cb.pack(anchor='w', side=tk.LEFT)
        self.restore_default_btn = ttk.Button(default_browser_frame, text="Reset to Defaults",
                                              command=self.reset_to_default_paths, width=15)
        self.restore_default_btn.pack(side=tk.LEFT, padx=10)

    def setup_buttons_frame(self) -> None:
        """Create the action buttons and status display."""
        self.buttons_frame = ttk.Frame(self.main_frame, style='TFrame')
        self.buttons_frame.pack(fill=tk.X, pady=(0, 10))
        self.status_label = ttk.Label(self.buttons_frame, text="", style='NormalStatus.TLabel')
        self.status_label.pack(side=tk.LEFT, padx=10)
        spacer = ttk.Label(self.buttons_frame, text="", style='TFrame')
        spacer.pack(side=tk.LEFT, fill=tk.X, expand=True)

        button_container = ttk.Frame(self.buttons_frame, style='TFrame')
        button_container.pack(side=tk.RIGHT, padx=5)
        self.btn_clear = ttk.Button(button_container, text="Clear Form", command=self.clear_form, width=10)
        self.btn_clear.pack(side=tk.LEFT, padx=(0, 8))
        self.btn_add = ttk.Button(button_container, text="Add Account", command=self.run_add_new_account,
                                  style='Accent.TButton', width=12)
        self.btn_add.pack(side=tk.LEFT, padx=(0, 8))
        self.btn_cancel = ttk.Button(button_container, text="Cancel", command=self.cancel_current_process, width=8)
        self.btn_cancel.pack(side=tk.LEFT)
        self.btn_cancel.config(state=tk.DISABLED)

    def setup_output_frame(self) -> None:
        """Create the frame for displaying process logs and messages."""
        self.output_frame = ttk.Frame(self.main_frame, style='Card.TFrame')
        self.output_frame.pack(fill=tk.BOTH, expand=True)
        self.output_frame['borderwidth'] = 1
        self.output_frame['relief'] = 'solid'

        output_header = ttk.Frame(self.output_frame, style='Card.TFrame')
        output_header.pack(fill=tk.X, padx=10, pady=5)
        console_label = tk.Label(output_header, text="> Console Output",
                                 font=('Segoe UI', 11, 'bold'), background=COLOR_FRAME_BG,
                                 foreground=COLOR_PRESENCE_DARK)
        console_label.pack(anchor='w')

        output_frame_with_scrollbar = ttk.Frame(self.output_frame, style='Card.TFrame')
        output_frame_with_scrollbar.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))
        self.output_text = tk.Text(output_frame_with_scrollbar, wrap="word", height=40,
                                    font=('Consolas', 9), background=COLOR_FRAME_BG,
                                    borderwidth=0, padx=10, pady=5)
        self.output_text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar = ttk.Scrollbar(output_frame_with_scrollbar, orient="vertical", command=self.output_text.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.output_text.configure(yscrollcommand=scrollbar.set)

        self.output_text.tag_configure("success", foreground=COLOR_SUCCESS)
        self.output_text.tag_configure("error", foreground=COLOR_ERROR)
        self.output_text.tag_configure("header", font=('Consolas', 9, 'bold'), foreground=COLOR_PRESENCE_DARK)
        self.output_text.tag_configure("command", foreground="#0066cc")
        self.output_text.tag_configure("warning", foreground="#FF9800")
        self.output_text.tag_configure("info", foreground="#2196F3")
        self.output_text.tag_configure("info-alt", foreground="#00BCD4")

        self.display_welcome_message()

    def display_welcome_message(self) -> None:
        """
        Clear the output text and display a welcome message.
        """
        welcome_message = (
            "Presence - Social Media Account Authorization\n\n"
            "This tool allows you to authorize YouTube and TikTok accounts and store their credentials.\n\n"
            "Instructions:\n"
            "1. Enter the account email address\n"
            "2. Select the platform (YouTube or TikTok)\n"
            "3. Verify the credentials file path\n"
            "4. Click 'Add Account' to start the authorization process\n\n"
            "The authorization process will open a browser window where you'll need to sign in.\n"
            "All process logs will appear in this console output area.\n"
        )
        self.output_text.config(state=tk.NORMAL)
        self.output_text.delete("1.0", tk.END)
        self.output_text.insert(tk.END, welcome_message, "header")
        self.output_text.config(state=tk.DISABLED)

    def parse_ansi_colors(self, text: str) -> List[Tuple[str, Optional[str]]]:
        """
        Parse ANSI escape sequences (e.g., \x1b[31m, \x1b[1;34m, \x1b[0m) and return (segment, tag) pairs.

        :param text: The text containing ANSI codes.
        :return: A list of (segment, tag) tuples.
        """
        ANSI_PATTERN = re.compile(r'\x1b\[(?P<codes>[0-9;]*?)m')
        segments: List[Tuple[str, Optional[str]]] = []
        last_end = 0
        current_tag: Optional[str] = None

        def codes_to_tag(codes_str: str) -> Optional[str]:
            codes = codes_str.split(';')
            for code in codes:
                if code == '0':
                    return None  # Reset tag.
                elif code == '1':
                    continue  # Bold flag; ignore on its own.
                elif code in ('91', '31'):
                    return "error"
                elif code in ('92', '32'):
                    return "success"
                elif code in ('93', '33'):
                    return "warning"
                elif code in ('94', '34'):
                    return "info"
                elif code in ('95', '35'):
                    return "header"
                elif code == '36':
                    return "info-alt"
            return current_tag

        for match in ANSI_PATTERN.finditer(text):
            start, end = match.span()
            if start > last_end:
                segments.append((text[last_end:start], current_tag))
            codes_str = match.group('codes')
            current_tag = codes_to_tag(codes_str)
            last_end = end
        if last_end < len(text):
            segments.append((text[last_end:], current_tag))
        return segments

    def update_creds_path(self, event: Optional[Any] = None) -> None:
        """
        Update the credentials file path and change the platform indicator color.
        """
        platform = self.combo_platform.get().lower()
        if platform == "youtube":
            self.platform_indicator.config(background=COLOR_YOUTUBE)
            new_path = str(Path(self.credentials_folder_base) / self.creds_youtube_file)
        else:
            self.platform_indicator.config(background=COLOR_TIKTOK)
            new_path = str(Path(self.credentials_folder_base) / self.creds_tiktok_file)
        self.entry_creds.delete(0, tk.END)
        self.entry_creds.insert(0, new_path)

    def browse_creds_file(self) -> None:
        """Open a file dialog to select a credentials file."""
        filename = filedialog.askopenfilename(
            title="Select Credentials File",
            filetypes=(("JSON files", "*.json"), ("All files", "*.*")),
            initialdir=str(Path(self.entry_creds.get()).parent)
        )
        if filename:
            self.entry_creds.delete(0, tk.END)
            self.entry_creds.insert(0, filename)

    def browse_chrome_path(self) -> None:
        """Open a file dialog to select a Chrome executable."""
        filename = filedialog.askopenfilename(
            title="Select Chrome Executable",
            filetypes=(("EXE files", "*.exe"), ("All files", "*.*")),
            initialdir=str(Path(self.entry_chrome.get()).parent)
        )
        if filename:
            self.entry_chrome.delete(0, tk.END)
            self.entry_chrome.insert(0, filename)

    def toggle_default_browser(self) -> None:
        """Toggle between a custom Chrome path and the default browser."""
        if self.use_default_browser_var.get():
            self.chrome_path_cache = self.entry_chrome.get()
            self.entry_chrome.config(state='disabled')
            self.browse_chrome_btn.config(state='disabled')
        else:
            self.entry_chrome.config(state='normal')
            self.browse_chrome_btn.config(state='normal')
            self.entry_chrome.delete(0, tk.END)
            if self.chrome_path_cache:
                self.entry_chrome.insert(0, self.chrome_path_cache)
            elif self.authorization_refresh_chrome_path:
                self.entry_chrome.insert(0, self.authorization_refresh_chrome_path)

    def clear_form(self) -> None:
        """Clear all form inputs and reset the output area."""
        self.entry_account.delete(0, tk.END)
        self.combo_platform.current(0)
        self.entry_creds.delete(0, tk.END)
        self.entry_chrome.config(state='normal')
        self.entry_chrome.delete(0, tk.END)
        self.use_default_browser_var.set(False)
        self.browse_chrome_btn.config(state='normal')
        self.chrome_path_cache = None
        self.status_label.configure(style='NormalStatus.TLabel', text="Form cleared")
        logger.info("Form cleared by user.")
        self.display_welcome_message()

    def reset_to_default_paths(self) -> None:
        """Reset the credentials file and Chrome path to their default values."""
        platform = self.combo_platform.get().lower()
        if platform == "youtube":
            creds_file = str(Path(self.credentials_folder_base) / self.creds_youtube_file)
        else:
            creds_file = str(Path(self.credentials_folder_base) / self.creds_tiktok_file)
        self.entry_creds.delete(0, tk.END)
        self.entry_creds.insert(0, creds_file)
        if not self.use_default_browser_var.get() and self.authorization_refresh_chrome_path:
            self.entry_chrome.delete(0, tk.END)
            self.entry_chrome.insert(0, self.authorization_refresh_chrome_path)
            self.chrome_path_cache = self.authorization_refresh_chrome_path
        logger.info("Default paths restored.")

    def read_process_output(self, process: subprocess.Popen, output_queue: queue.Queue) -> None:
        """
        Read output from the inner refresh-auth executable and place each line in a queue.
        ANSI escape sequences are retained for GUI display.
        """
        try:
            while True:
                output = process.stdout.readline()
                if output == '' and process.poll() is not None:
                    break
                if output:
                    logger.debug("Inner exe output: %s", output.strip())
                    output_queue.put(output)
            remainder = process.stdout.read()
            if remainder:
                logger.debug("Inner exe remainder output.")
                output_queue.put(remainder)
            output_queue.put(f"RETURN_CODE:{process.poll()}")
        except OSError as e:
            logger.info("OS error while reading process output: %s", e)
        except Exception as e:
            logger.info("Error while reading process output: %s", e)

    def update_output_from_queue(self, output_queue: queue.Queue, process: subprocess.Popen, start_time: float) -> None:
        """
        Retrieve output lines from the queue, parse ANSI colors, and insert them into the GUI output.
        When the process terminates, log a clear separator in the log file.
        """
        try:
            while True:
                try:
                    line = output_queue.get_nowait()
                    line = line.rstrip('\n')
                    logger.debug("Processing line from inner exe: %s", line)
                    if line.startswith("RETURN_CODE:"):
                        return_code = int(line.split(":", 1)[1])
                        account = self.entry_account.get()
                        if return_code == 0:
                            status_str = "Success"
                            platform = self.combo_platform.get().lower()
                            self.status_label.configure(style='SuccessStatus.TLabel',
                                                        text=f"Success: {platform} authorized")
                            self.output_text.config(state=tk.NORMAL)
                            self.output_text.insert(tk.END,
                                                    f"\n✓ {platform.capitalize()} authorization for {account} completed successfully.\n\n",
                                                    "success")
                            self.output_text.config(state=tk.DISABLED)
                            logger.info("Authorization succeeded for account: %s", account)
                        elif return_code == 130:
                            status_str = "Cancelled"
                            platform = self.combo_platform.get().lower()
                            self.status_label.configure(style='WarningStatus.TLabel',
                                                        text=f"Cancelled: {platform} authorization cancelled by user")
                            self.output_text.config(state=tk.NORMAL)
                            self.output_text.insert(tk.END,
                                                    f"\n⚠ {platform.capitalize()} authorization for {account} was cancelled by the user.\n\n",
                                                    "warning")
                            self.output_text.config(state=tk.DISABLED)
                            logger.info("Authorization cancelled for account: %s", account)
                        else:
                            status_str = "Failure"
                            self.status_label.configure(style='ErrorStatus.TLabel',
                                                        text=f"Error: Authorization failed (code {return_code})")
                            self.output_text.config(state=tk.NORMAL)
                            self.output_text.insert(tk.END,
                                                    f"\n⚠ Process exited with code {return_code}\n\n",
                                                    "error")
                            self.output_text.config(state=tk.DISABLED)
                            logger.info("Authorization failed with return code: %d", return_code)

                        # Log a separator block for clarity.
                        self.log_refresh_termination(account, status_str, return_code)

                        self.btn_add.config(state=tk.NORMAL)
                        self.btn_cancel.config(state=tk.DISABLED)
                        self.current_process = None
                        return
                    else:
                        colored_segments = self.parse_ansi_colors(line)
                        self.output_text.config(state=tk.NORMAL)
                        for seg_text, seg_tag in colored_segments:
                            if seg_tag:
                                self.output_text.insert(tk.END, seg_text, seg_tag)
                            else:
                                self.output_text.insert(tk.END, seg_text)
                        self.output_text.insert(tk.END, "\n")
                        self.output_text.see(tk.END)
                        self.output_text.config(state=tk.DISABLED)
                except queue.Empty:
                    break
        except Exception as e:
            logger.info("Error updating output: %s", e)
            self.output_text.config(state=tk.NORMAL)
            self.output_text.insert(tk.END, f"Error updating output: {e}\n", "error")
            self.output_text.config(state=tk.DISABLED)
        if process and process.poll() is None:
            self.root.after(50, lambda: self.update_output_from_queue(output_queue, process, start_time))

    def log_refresh_termination(self, account: str, status: str, return_code: int) -> None:
        """
        Log a clear separator block plus a summary message when the inner executable terminates.

        :param account: The account identifier.
        :param status: A brief status text (Success, Cancelled, Failure).
        :param return_code: The return code from the process.
        """
        separator = "=" * 80
        logger.info(separator)
        logger.info("Refresh auth terminated for account: %s | Status: %s | Return code: %d",
                    account, status, return_code)
        logger.info(separator)

    def check_process_timeout(self, process: subprocess.Popen, start_time: float, timeout: int = 120) -> None:
        """
        Monitor the subprocess and terminate it if it exceeds the timeout.

        :param process: The subprocess.
        :param start_time: When the subprocess was started.
        :param timeout: Timeout (seconds).
        """
        if process and process.poll() is None:
            elapsed_time = time.time() - start_time
            logger.debug("Process running for %.2f seconds", elapsed_time)
            if elapsed_time > timeout:
                try:
                    process.terminate()
                    self.status_label.configure(style='ErrorStatus.TLabel',
                                                text="Error: Process timed out and was terminated")
                    self.output_text.config(state=tk.NORMAL)
                    self.output_text.insert(tk.END,
                                            f"\n⚠ Process timed out after {timeout} seconds and was terminated.\n\n",
                                            "error")
                    self.output_text.config(state=tk.DISABLED)
                    self.btn_add.config(state=tk.NORMAL)
                    self.btn_cancel.config(state=tk.DISABLED)
                    self.current_process = None
                    logger.info("Process terminated due to timeout.")
                    return
                except Exception as e:
                    logger.info("Error terminating process: %s", e)
            self.root.after(2000, lambda: self.check_process_timeout(process, start_time, timeout))
        else:
            self.btn_add.config(state=tk.NORMAL)
            self.btn_cancel.config(state=tk.DISABLED)
            self.current_process = None

    def run_add_new_account(self) -> None:
        """
        Validate the form, launch the refresh executable, and monitor its output.
        """
        account = self.entry_account.get().strip()
        platform = self.combo_platform.get().lower()
        creds_file = self.entry_creds.get().strip()
        use_default = self.use_default_browser_var.get()
        chrome_path = "" if use_default else self.entry_chrome.get().strip()

        if not account:
            self.status_label.configure(style='ErrorStatus.TLabel', text="Error: Missing email")
            self.output_text.config(state=tk.NORMAL)
            self.output_text.delete("1.0", tk.END)
            self.output_text.insert(tk.END, "Error: Account email is required.\n", "error")
            self.output_text.config(state=tk.DISABLED)
            logger.info("Missing account email; authorization aborted.")
            return

        if not creds_file:
            self.status_label.configure(style='ErrorStatus.TLabel', text="Error: Missing credentials path")
            self.output_text.config(state=tk.NORMAL)
            self.output_text.delete("1.0", tk.END)
            self.output_text.insert(tk.END, "Error: Credentials file path is required.\n", "error")
            self.output_text.config(state=tk.DISABLED)
            logger.info("Missing credentials file; authorization aborted.")
            return

        self.status_label.configure(style='NormalStatus.TLabel', text=f"Authorizing {account}...")
        self.output_text.config(state=tk.NORMAL)
        self.output_text.delete("1.0", tk.END)
        self.output_text.insert(tk.END, f"Starting {platform.capitalize()} authorization for {account}...\n\n")
        self.output_text.config(state=tk.DISABLED)
        self.root.update()
        logger.info("Starting authorization for account: %s on platform: %s", account, platform)

        cmd = [
            self.authorization_refresh_exe_path,
            "--platform", platform,
            "--account", account,
            "--creds_file_path", creds_file,
            "--timeout", "120",
            "--add_new_account"
        ]
        if chrome_path and not use_default:
            cmd.extend(["--chrome", chrome_path])

        try:
            self.btn_add.config(state=tk.DISABLED)
            self.btn_cancel.config(state=tk.NORMAL)
            self.output_text.config(state=tk.NORMAL)
            self.output_text.insert(tk.END, "Executing command:\n", "header")
            self.output_text.insert(tk.END, " ".join(cmd) + "\n\n")
            self.output_text.config(state=tk.DISABLED)
            self.root.update()

            start_time = time.time()
            output_queue: queue.Queue = queue.Queue()
            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                bufsize=1,
                universal_newlines=True
            )
            self.current_process = process
            logger.info("Launched subprocess (PID: %s) for account: %s", process.pid, account)
            output_thread = threading.Thread(target=self.read_process_output, args=(process, output_queue))
            output_thread.daemon = True
            output_thread.start()
            self.root.after(100, lambda: self.update_output_from_queue(output_queue, process, start_time))
            self.root.after(2000, lambda: self.check_process_timeout(process, start_time))
        except Exception as e:
            self.status_label.configure(style='ErrorStatus.TLabel', text="Error: Process failed")
            self.output_text.config(state=tk.NORMAL)
            self.output_text.delete("1.0", tk.END)
            self.output_text.insert(tk.END, f"Error running the authorization process:\n{str(e)}\n", "error")
            self.output_text.config(state=tk.DISABLED)
            self.btn_add.config(state=tk.NORMAL)
            self.btn_cancel.config(state=tk.DISABLED)
            self.current_process = None
            logger.info("Exception launching subprocess: %s", e)

    def cancel_current_process(self) -> None:
        """
        Cancel the running subprocess, if any.
        """
        if self.current_process and self.current_process.poll() is None:
            try:
                self.current_process.terminate()
                self.status_label.configure(style='ErrorStatus.TLabel', text="Authorization cancelled by user")
                self.output_text.config(state=tk.NORMAL)
                self.output_text.insert(tk.END, "\n⚠ Authorization process was cancelled by the user.\n\n", "warning")
                self.output_text.config(state=tk.DISABLED)
                self.btn_add.config(state=tk.NORMAL)
                self.btn_cancel.config(state=tk.DISABLED)
                self.current_process = None
                logger.info("Authorization process cancelled by user.")
            except Exception as e:
                logger.info("Error cancelling process: %s", e)

    def run(self) -> None:
        """Launch the Tkinter main loop."""
        self.root.mainloop()

###############################################################################
# ENTRY POINT
###############################################################################
def main():
    parser = argparse.ArgumentParser(description="Authorize social media accounts using Presence.")
    # Required positional argument for appsettings.
    parser.add_argument("appsettings", type=Path,
                        help="Path to the appsettings.json file. This argument is required.")
    # Optional log file argument.
    parser.add_argument("--log", dest="log_file", type=Path, default=None,
                        help="Optional log file path. If not provided, a 'Logs' folder with a timestamped filename is used.")
    args = parser.parse_args()

    if not args.appsettings.is_file():
        logger.info("Error: The specified appsettings file does not exist: %s", args.appsettings)
        sys.exit(1)

    try:
        with args.appsettings.open("r", encoding="utf-8") as f:
            appsettings = json.load(f)
    except Exception as e:
        logger.info("Error reading appsettings file: %s", e)
        sys.exit(1)

    log_file_path = get_log_file_path(args.log_file)
    logger.info("Log file: %s", log_file_path)

    app = PresenceAuthApp(appsettings)
    app.run()


if __name__ == "__main__":
    main()