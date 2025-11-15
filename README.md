# Transformation Form7 → Devis.aspx

## 📋 Description

Ce projet est une **transformation complète** de l'application Windows Forms **Form7.vb** en application web **ASP.NET WebForms** avec les fichiers **Devis.aspx** et **Devis.aspx.vb**.

## 🎯 Objectif

Convertir une application de bureau de gestion de devis en une application web moderne, accessible depuis un navigateur, tout en conservant toutes les fonctionnalités principales.

---

## 📁 Structure des Fichiers

```
/Devis/
├── Devis.aspx              # Page web (interface utilisateur)
├── Devis.aspx.vb           # Code-behind (logique métier)
├── Web.config              # Configuration ASP.NET
├── Devis bureau.txt        # Fichier source original (Form7.vb)
└── README.md               # Ce fichier
```

---

## 🔄 Correspondances Windows Forms → ASP.NET

### Contrôles Transformés

| Windows Forms | ASP.NET WebForms | Description |
|---------------|------------------|-------------|
| `DataGridView dgvListeDevis` | `GridView dgvListeDevis` | Liste des devis |
| `DataGridView dgvLignesDevis` | `GridView dgvLignesDevis` | Lignes du devis |
| `ComboBox cmbProjetDevis` | `DropDownList cmbProjetDevis` | Sélection projet |
| `ComboBox cmbStatutDevis` | `DropDownList cmbStatutDevis` | Sélection statut |
| `ComboBox cmbDesignationLigneDevis` | `DropDownList cmbDesignationLigneDevis` | Sélection tâche |
| `TextBox txtNumeroDevis` | `TextBox txtNumeroDevis` | Numéro de devis |
| `TextBox txtRechercheDevis` | `TextBox txtRechercheDevis` | Recherche temps réel |
| `DateTimePicker dtpDateDevis` | `TextBox dtpDateDevis (TextMode="Date")` | Sélection de date |
| `Button btnNouveauDevis` | `Button btnNouveauDevis` | Bouton Nouveau |
| `Label lblCompteurDevis` | `Label lblCompteurDevis` | Compteur de devis |

### Événements Transformés

| Windows Forms | ASP.NET WebForms | Type |
|---------------|------------------|------|
| `Form7_Load` | `Page_Load` | Chargement initial |
| `btnNouveauDevis_Click` | `btnNouveauDevis_Click` | Événement serveur |
| `dgvListeDevis_SelectionChanged` | `dgvListeDevis_SelectedIndexChanged` | Sélection ligne |
| `cmbDesignationLigneDevis_SelectedIndexChanged` | `cmbDesignationLigneDevis_SelectedIndexChanged` | AutoPostBack |
| `txtRechercheDevis_TextChanged` | `txtRechercheDevis_TextChanged` | AutoPostBack |

---

## ⚙️ Fonctionnalités Implémentées

### ✅ Gestion CRUD Complète
- ✅ **Création** de nouveaux devis avec génération automatique du numéro
- ✅ **Lecture** et affichage des devis existants
- ✅ **Modification** des devis (avec validation de statut)
- ✅ **Suppression** des devis (avec confirmation)

### ✅ Gestion des Lignes
- ✅ Ajout de lignes de tâches
- ✅ Suppression de lignes
- ✅ Calcul automatique des montants (Quantité × Prix Unitaire)
- ✅ Calcul du total HT
- ✅ Pré-remplissage depuis les tâches prédéfinies

### ✅ Gestion des Sections
- ✅ Création de sections groupant plusieurs lignes
- ✅ Calcul automatique des sous-totaux de sections
- ✅ Modification de sections
- ✅ Suppression de sections (libération des lignes)
- ✅ Ajout de titres

### ✅ Recherche et Filtrage
- ✅ Recherche temps réel (numéro, projet, statut, objet)
- ✅ Filtrage par statut (Brouillon, Envoyé, Déposé, etc.)
- ✅ Compteur dynamique de résultats

### ✅ Génération PDF
- ✅ Génération de devis au format PDF avec iTextSharp
- ✅ En-tête et informations client
- ✅ Tableau des lignes avec sections
- ✅ Sous-totaux de sections
- ✅ Montant total HT
- ✅ Montant en lettres
- ✅ Signature

### ✅ Export et Statistiques
- ✅ Export Excel de la liste des devis
- ✅ Statistiques complètes (nombre par statut, montants, moyennes)
- ✅ Compteur en temps réel (nombre total + montant total)

### ✅ Workflow
- ⚠️ **Dépôt de devis** (structure prête, nécessite Form6)
- ⚠️ **Retour client** (structure prête, nécessite Form18)
- ✅ **Validation de statut** (empêche modifications selon statut)
- ✅ **Création de facture** (vérifications implémentées)

### ✅ Validations
- ✅ Validation des champs obligatoires
- ✅ Validation des montants et quantités
- ✅ Vérification de la possibilité de modification
- ✅ Empêche modification des devis facturés/commandés
- ✅ Vérification des doublons de factures

---

## 🗄️ Structure de Base de Données

### Tables Utilisées

#### 1. **DEVIS**
```sql
CREATE TABLE DEVIS (
    DevisID INT PRIMARY KEY IDENTITY(1,1),
    NumeroDevis NVARCHAR(50) NOT NULL,
    NumeroChrono NVARCHAR(10),
    ProjetID INT,
    DateDevis DATE,
    StatutDevis NVARCHAR(20),
    ObjetDevis NVARCHAR(MAX),
    MontantHT DECIMAL(18,2),
    FOREIGN KEY (ProjetID) REFERENCES Projets(ProjetID)
)
```

#### 2. **LIGNESDEVIS**
```sql
CREATE TABLE LIGNESDEVIS (
    LigneDevisID INT PRIMARY KEY IDENTITY(1,1),
    DevisID INT NOT NULL,
    Designation NVARCHAR(MAX),
    Unite NVARCHAR(50),
    Quantite DECIMAL(18,2),
    PrixUnitaire DECIMAL(18,2),
    MontantLigne DECIMAL(18,2),
    TypeLigne NVARCHAR(20), -- LIGNE, SECTION, TITRE
    SectionNom NVARCHAR(255),
    OrdreAffichage INT,
    FOREIGN KEY (DevisID) REFERENCES DEVIS(DevisID)
)
```

#### 3. **Projets**
```sql
CREATE TABLE Projets (
    ProjetID INT PRIMARY KEY IDENTITY(1,1),
    NomProjet NVARCHAR(255),
    NumeroProjet NVARCHAR(50),
    ClientID INT,
    AppelClientID INT,
    StatutProjet NVARCHAR(50),
    FOREIGN KEY (ClientID) REFERENCES Clients(ClientID)
)
```

#### 4. **TachesPredefinies**
```sql
CREATE TABLE TachesPredefinies (
    TacheID INT PRIMARY KEY IDENTITY(1,1),
    Designation NVARCHAR(MAX),
    Unite NVARCHAR(50),
    PrixUnitaire DECIMAL(18,2)
)
```

#### 5. **Clients**
```sql
CREATE TABLE Clients (
    ClientID INT PRIMARY KEY IDENTITY(1,1),
    NomClient NVARCHAR(255),
    Adresse NVARCHAR(MAX),
    Telephone NVARCHAR(50),
    Email NVARCHAR(255)
)
```

---

## 🚀 Installation et Configuration

### Prérequis

1. **Serveur Web IIS** (Windows Server ou IIS Express)
2. **SQL Server** (2016 ou supérieur)
3. **.NET Framework 4.8**
4. **Bibliothèques NuGet** :
   - `iTextSharp` (pour génération PDF)
   - `System.Data.SqlClient`

### Étapes d'Installation

#### 1. Configurer la Base de Données

```sql
-- Créer la base de données
CREATE DATABASE GestionDevis;
GO

USE GestionDevis;
GO

-- Exécuter les scripts de création des tables (voir section précédente)
-- Insérer des données de test
```

#### 2. Modifier Web.config

Ouvrir `Web.config` et modifier la chaîne de connexion :

```xml
<connectionStrings>
  <add name="DefaultConnection"
       connectionString="Data Source=VOTRE_SERVEUR;Initial Catalog=GestionDevis;Integrated Security=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

#### 3. Créer le Fichier DbHelper.vb

Créer un fichier séparé `DbHelper.vb` avec la classe utilitaire pour les accès à la base de données (code inclus dans Devis.aspx.vb, section `#Region "Classe DbHelper"`).

#### 4. Publier l'Application

**Via Visual Studio :**
1. Ouvrir le projet dans Visual Studio
2. Clic droit sur le projet → **Publier**
3. Sélectionner **IIS, FTP, etc.**
4. Configurer le profil de publication
5. Publier

**Manuellement :**
1. Copier tous les fichiers dans `C:\inetpub\wwwroot\Devis\`
2. Créer une application IIS pointant vers ce dossier
3. Configurer le pool d'applications (.NET Framework 4.8)

#### 5. Installer iTextSharp

```powershell
Install-Package iTextSharp -Version 5.5.13.3
```

Ou télécharger manuellement et référencer la DLL.

---

## 📊 Différences Clés Windows Forms vs ASP.NET

### 1. **Gestion de l'État**
- **Windows Forms** : État persistant dans les variables de classe
- **ASP.NET** : Utilisation de **ViewState** pour persister les données entre postbacks

```vb
' Windows Forms
Private devisID As Integer = 0

' ASP.NET
Private Property CurrentDevisID As Integer
    Get
        Return If(ViewState("CurrentDevisID"), 0)
    End Get
    Set(value As Integer)
        ViewState("CurrentDevisID") = value
    End Set
End Property
```

### 2. **Modèle d'Événements**
- **Windows Forms** : Événements client (instantanés)
- **ASP.NET** : **PostBack** vers le serveur (nécessite `AutoPostBack="true"`)

```xml
<!-- AutoPostBack pour déclencher un événement serveur -->
<asp:DropDownList ID="cmbProjetDevis" runat="server" AutoPostBack="true" />
```

### 3. **Rafraîchissement de l'Interface**
- **Windows Forms** : Appel direct aux méthodes (ex: `dgv.Refresh()`)
- **ASP.NET** : **DataBind()** pour lier les données

```vb
' ASP.NET
dgvLignesDevis.DataSource = TableLignesDevis
dgvLignesDevis.DataBind()
```

### 4. **Messages Utilisateur**
- **Windows Forms** : `MessageBox.Show()`
- **ASP.NET** : Labels avec classes CSS Bootstrap

```vb
Private Sub AfficherMessage(message As String, type As String)
    lblMessage.Text = message
    lblMessage.CssClass = "alert alert-" & type
    lblMessage.Visible = True
End Sub
```

### 5. **Téléchargement de Fichiers**
- **Windows Forms** : `SaveFileDialog`
- **ASP.NET** : `Response.BinaryWrite()` et `Response.End()`

```vb
Response.Clear()
Response.ContentType = "application/pdf"
Response.AddHeader("Content-Disposition", "attachment; filename=Devis.pdf")
Response.BinaryWrite(pdfBytes)
Response.End()
```

---

## 🎨 Design et Interface

### Technologies Utilisées
- **Bootstrap 5.3** : Framework CSS responsive
- **Font Awesome 6.4** : Icônes
- **CSS personnalisé** : Badges de statut colorés

### Badges de Statut

| Statut | Couleur | Classe CSS |
|--------|---------|------------|
| Brouillon | Gris | `statut-brouillon` |
| Envoyé | Bleu | `statut-envoye` |
| Déposé | Orange | `statut-depose` |
| Accepté | Vert | `statut-accepte` |
| Refusé | Rouge | `statut-refuse` |
| Révision | Orange foncé | `statut-revision` |
| Commandé | Violet | `statut-commande` |
| Facturé | Turquoise | `statut-facture` |

---

## 🔧 Améliorations Futures

### Fonctionnalités à Développer

1. **Workflow Complet**
   - [ ] Créer Form6.aspx (Dépôt de devis)
   - [ ] Créer Form18.aspx (Retour client)
   - [ ] Intégration complète du workflow automatique

2. **Améliorations Interface**
   - [ ] Pagination côté serveur pour grandes listes
   - [ ] Tri dynamique des colonnes
   - [ ] Modals Bootstrap pour les formulaires
   - [ ] Notifications Toast au lieu de labels

3. **Sécurité**
   - [ ] Authentification utilisateur
   - [ ] Gestion des rôles (Admin, Utilisateur)
   - [ ] Audit trail (historique des modifications)
   - [ ] Protection CSRF

4. **Performance**
   - [ ] Mise en cache des données fréquentes
   - [ ] Lazy loading des lignes
   - [ ] Compression Gzip
   - [ ] CDN pour Bootstrap/Font Awesome

5. **Export Avancé**
   - [ ] Export Excel avec formatage (xlColor, bordures)
   - [ ] Export multi-feuilles avec statistiques
   - [ ] Templates PDF personnalisables

6. **Notifications**
   - [ ] Envoi email automatique lors du changement de statut
   - [ ] Rappels pour devis en attente
   - [ ] Notifications temps réel (SignalR)

---

## 📝 Notes Importantes

### Limitations Actuelles

1. **Dépôt/Retour de Devis**
   - Les boutons "Déposer" et "Retour" affichent un message informatif
   - Nécessite la création des pages Form6.aspx et Form18.aspx

2. **Conversion Montant en Lettres**
   - Implémentation simplifiée
   - Supporte les millions, milliers et centaines
   - À compléter pour cas complexes (soixante-dix, quatre-vingt-dix, etc.)

3. **Validation Côté Client**
   - Validation côté serveur uniquement
   - Ajouter validation JavaScript pour meilleure UX

4. **Export Excel**
   - Export simple sans formatage
   - Utilise HtmlTextWriter (basique)

### Points d'Attention

- **ViewState** : Peut devenir volumineux avec beaucoup de lignes
  - Solution : Stocker dans Session ou base temporaire

- **PostBack** : Chaque action recharge la page
  - Solution : Utiliser **UpdatePanel** pour AJAX partiel

- **Sécurité SQL** : Utilise des paramètres SQL (protection injection)
  - Toujours utiliser `@ParamName` dans les requêtes

---

## 🤝 Support et Contact

Pour toute question ou amélioration, contacter l'équipe de développement.

---

## 📜 Licence

Propriété de **VERNET/KAV** - Usage interne uniquement

---

## 🏆 Changelog

### Version 1.0 - 15 Novembre 2025
- ✅ Transformation initiale Form7 → Devis.aspx
- ✅ CRUD complet des devis
- ✅ Gestion des lignes et sections
- ✅ Génération PDF avec iTextSharp
- ✅ Recherche et filtrage temps réel
- ✅ Export Excel basique
- ✅ Statistiques complètes
- ✅ Interface Bootstrap responsive

---

**Fin du README**
