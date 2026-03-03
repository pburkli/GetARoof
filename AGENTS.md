# Repository Guidelines

## Project Structure & Module Organization
This repository is currently planning-first (requirements and integration tracking), not yet an application codebase.

- Root docs: `requirements.md`, `GetARoof_Project_Idea.md`, `idea.txt`
- Environment helper: `load-env.ps1`
- Platform tracking notes: `platforms/` (for partner/API onboarding status)
- Sensitive local config: `.env` (ignored by git)

When implementation starts, place runtime code under `src/` and tests under `tests/` to keep concerns separated.

## Build, Test, and Development Commands
There is no build pipeline yet. Use the current utility commands below:

- `. .\load-env.ps1` loads variables from `.env` into the current PowerShell session.
- `. .\load-env.ps1 .\path\to\custom.env` loads a specific env file.
- `rg --files` lists tracked project files quickly.
- `rg "pattern"` searches requirements/notes for specific terms.

After .NET scaffolding is added, standardize on `dotnet restore`, `dotnet build`, and `dotnet test`.

## Coding Style & Naming Conventions
For new code (planned stack: Blazor WASM + ASP.NET Core):

- Use 4-space indentation and UTF-8 text files.
- C#: `PascalCase` for types/methods, `camelCase` for locals/parameters, one public type per file.
- PowerShell: approved verb-noun naming for functions, clear parameter names (as in `load-env.ps1`).
- Keep modules small and platform-specific logic behind adapters (see `requirements.md` architecture constraints).

## Testing Guidelines
No automated tests exist yet; contributors should add tests with features.

- Prefer xUnit for .NET projects.
- Test paths: `tests/<ProjectName>.Tests/`
- Test naming: `MethodName_State_ExpectedResult`
- Minimum expectation for PRs: tests for critical parsing, adapter behavior, and booking-flow orchestration logic.

## Commit & Pull Request Guidelines
Recent commit style is short, imperative, and lowercase (e.g., `add requirements`, `load-env`).

- Commit message format: concise subject, optional scope with `:` when useful.
- Keep commits focused; avoid mixing docs, refactors, and feature work unnecessarily.
- PRs should include: purpose summary, key changes, validation steps, and linked issue/task.
- For UI/workflow changes, include screenshots or sample request/response snippets.

## Security & Configuration Tips
- Never commit secrets. `.env`, `*key*`, and `*secret*` are ignored for this reason.
- Do not store raw credentials in docs under `platforms/`; keep only status notes.
- Payment data must never be persisted or logged (explicit product constraint in `requirements.md`).
