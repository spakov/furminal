# 🚀 Developer Workflow — Git + Nerdbank.GitVersioning (NBGV)

This project uses **Nerdbank.GitVersioning** to handle build version numbers automatically from Git commit history and tags.

---

## 🔹 Branch Naming

- `main` → always deployable.
- `feature/xyz` → new features.
- `bugfix/xyz` → bug fixes.
- `hotfix/xyz` → urgent patch from production.
- `release/vX.Y` → branch for preparing a stable release.

---

## 🔹 Typical Feature Flow

```bash
# 1️⃣ Make sure main is up to date
git checkout main
git pull origin main

# 2️⃣ Create a feature branch
git checkout -b feature/my-new-feature
git push -u origin feature/my-new-feature

# 3️⃣ Do your work!
# Commit and push regularly
git add .
git commit -m "Add something cool"
git push

# 4️⃣ Rebase to stay fresh
git fetch origin
git rebase origin/main
git push --force-with-lease

# 5️⃣ Merge to main when done
git checkout main
git pull origin main
git merge --no-ff feature/my-new-feature
git push origin main

# 6️⃣ Clean up
git branch -d feature/my-new-feature
git push origin --delete feature/my-new-feature

# 7️⃣ Create a release branch
nbgv prepare-release

# This pins the version (removes -alpha) and creates `release/vX.Y`

# 8️⃣ Finalize, test, tag
git checkout release/vX.Y
git tag vX.Y.0
git push origin release/vX.Y
git push origin vX.Y.0

# 9️⃣ Merge the release branch back to main
git checkout main
git merge release/vX.Y
git push origin main

# 1️⃣0️⃣ Bump main to next version
nbgv set-version X.Y+1
git commit -am "Bump version to X.Y+1"
git push origin main

# See what version your current branch builds
nbgv get-version
```
