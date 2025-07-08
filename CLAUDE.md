# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TrackForge is a DJ-focused C# wrapper around ffmpeg for batch audio file conversion. The program addresses limitations of batch file implementations, converting various audio formats to MP3 with robust Unicode and special character support, optimized for DJ track management workflows.

## Development Commands

### Build and Run
```bash
dotnet build
dotnet run
```

### Dependencies
- .NET 9.0 or higher
- ffmpeg (must be in PATH)
- ffprobe (usually included with ffmpeg)
- Newtonsoft.Json package (already included)

## Architecture

### Core Components

**Program.cs**: Single-file console application containing:
- `Main()`: Entry point with initialization sequence
- `LoadConfig()`: JSON configuration file management with auto-generation
- `CheckFFmpeg()`: Validates ffmpeg availability in PATH
- `ProcessAudioFiles()`: Orchestrates file discovery and conversion across multiple directories
- `ProcessFile()`: Handles individual file processing with metadata extraction
- `GetTrackTitle()`: Extracts track title from metadata using ffprobe
- `SanitizeFileName()`: Sanitizes file names for filesystem compatibility
- `ConvertWithFFmpeg()`: Executes ffmpeg conversion with detailed error handling
- `Config` class: Configuration model for JSON deserialization

### Key Design Patterns

**Configuration-First Approach**: 
- Auto-generates `config.json` on first run if missing
- Validates all dependencies (ffmpeg, directories) before processing
- Centralized configuration through `Config` class

**Batch Processing Flow**:
1. Load/validate configuration
2. Check ffmpeg/ffprobe availability
3. Validate all source directories
4. Recursive directory traversal for supported formats across multiple directories
5. Per-file metadata extraction and filename determination
6. Per-file conversion with detailed logging
7. Comprehensive results reporting

**Error Handling Strategy**:
- Graceful degradation with detailed error messages
- Comprehensive logging to `conversion.log`
- Separate tracking of skipped vs. failed files
- UTF-8 console output for Unicode file names

### File Processing Logic

The converter processes files by:
1. Scanning multiple source directories recursively for supported formats
2. Extracting track title from metadata using ffprobe
3. Determining output filename (metadata title or original filename)
4. Sanitizing filenames for filesystem compatibility
5. Maintaining directory structure in output
6. Skipping existing output files
7. Converting to MP3 with metadata and thumbnail preservation
8. Logging all operations and errors

### Configuration Schema

```json
{
  "SourceDirectories": ["path/to/source1", "path/to/source2"],
  "OutputDirectory": "path/to/output", 
  "Bitrate": "256k",
  "SupportedFormats": ["*.flac", "*.wav", "*.m4a", "*.aac", "*.ogg", "*.wma", "*.ape", "*.mp3"]
}
```

## Important Implementation Details

- Uses `Process` class for ffmpeg/ffprobe execution with proper stream handling
- Implements long path support for Windows
- Supports multiple source directories in a single conversion run
- Extracts metadata using ffprobe to determine optimal output filenames
- Sanitizes filenames to ensure filesystem compatibility
- Preserves directory structure during conversion
- Preserves album artwork and thumbnails in output files
- Handles Unicode file names correctly
- Comprehensive logging with timestamps and error details
- Async/await pattern for file I/O operations