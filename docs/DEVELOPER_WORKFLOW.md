# 🚀 Developer Workflow — Git + Nerdbank.GitVersioning (NBGV)
This project uses **Nerdbank.GitVersioning** to handle build version numbers automatically from Git commit history and tags.

We maintain a single active release branch (e.g., `release/v1.0`). All day‑to‑day development happens off this branch. `main` exists primarily for historical merges and version bumps—it is not deployed from directly.

**Resist the urge to merge release branches into `main` until we're ready to move to the next version.**

## 🔹 Branch Naming
- `release/vX.Y` → **the release branch** (only one active at a time).
- `feature/xyz` → new feature branches (cut from the release branch).
- `bugfix/xyz` → bug fix branches (cut from the release branch).
- `main` → used only for bookkeeping merges after releases, not for active dev.

## 🔹 Typical Workflow
```bash
# 1️⃣ Make sure the release branch is up to date
git checkout release/v${X}.${Y}
git pull origin release/v${X}.${Y}

# 2️⃣ Create a feature or bug fix branch from the release branch
git checkout -b feature/my-new-feature
git push -u origin feature/my-new-feature

# 3️⃣ Do your work
git add .
git commit -m "Add something cool"
git push

# 4️⃣ Rebase frequently to stay up to date with the release branch
git fetch origin
git rebase origin/release/v${X}.${Y}
git push --force-with-lease

# 5️⃣ Merge into the release branch when done
git checkout release/v${X}.${Y}
git pull origin release/v${X}.${Y}
git merge --no-ff feature/my-new-feature
git push origin release/v${X}.${Y}

# 6️⃣ Clean up
git branch -d feature/my-new-feature
git push origin --delete feature/my-new-feature
```

## 🔹 Publishing a Release
```bash
# 1️⃣ Make sure the release branch is up to date
git checkout release/v${X}.${Y}
git pull origin release/v${X}.${Y}

# 2️⃣ Finalize and test

# Confirm the version:
nbgv get-version

# 3️⃣ Create a tag using NBGV
nbgv tag

# This prints the tag matching the current version, e.g. v${X}.${Y}.${Z}

# 4️⃣ Push the tag
git push origin v${X}.${Y}.${Z}
```

## 🔹 Moving to the Next Version
```bash
# 1️⃣ Make sure the current release branch is up to date
git checkout release/v${X}.${Y}
git pull origin release/v${X}.${Y}

# 2️⃣ Create the next release branch
nbgv prepare-release --no-merge

# This bumps the current branch to a release version and creates the new release branch (e.g. release/v${X}.$((Y + 1)))

# 3️⃣ Push the new release branch
git checkout release/v${X}.$((Y + 1))
git push origin release/v${X}.$((Y + 1))

# 4️⃣ Merge the release branch into main for record-keeping
git checkout main
git merge release/v${X}.$((Y + 1))
git push origin main

# 5️⃣ Switch back to the new release branch for ongoing work
git checkout release/v${X}.$((Y + 1))

# Verify version
nbgv get-version
```

## 🔹 Summary of Flow
- Work happens on feature/bug fix branches off the active release branch.
- Tags are created and pushed on the release branch when you’re ready to publish.
- `main` is maintained for history and bookkeeping but is not a source of deployable builds.
