<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Devis.aspx.vb" Inherits="GestionDevis.Devis" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <title>Gestion des Devis</title>

    <!-- Bootstrap CSS -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet"/>
    <!-- Font Awesome -->
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"/>

    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f5f5f5;
        }
        .main-container {
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            padding: 20px;
            margin-top: 20px;
        }
        .section-title {
            color: #2c3e50;
            border-bottom: 2px solid #3498db;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        .btn-action {
            margin: 5px;
            min-width: 120px;
        }
        .statut-badge {
            padding: 5px 10px;
            border-radius: 4px;
            font-weight: bold;
        }
        .statut-brouillon { background-color: #95a5a6; color: white; }
        .statut-envoye { background-color: #3498db; color: white; }
        .statut-depose { background-color: #f39c12; color: white; }
        .statut-accepte { background-color: #27ae60; color: white; }
        .statut-refuse { background-color: #e74c3c; color: white; }
        .statut-revision { background-color: #e67e22; color: white; }
        .statut-commande { background-color: #9b59b6; color: white; }
        .statut-facture { background-color: #16a085; color: white; }
        .compteur-devis {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        .grid-container {
            max-height: 400px;
            overflow-y: auto;
            border: 1px solid #ddd;
            border-radius: 4px;
        }
        .form-section {
            background-color: #f8f9fa;
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 15px;
        }
        .required-field::after {
            content: " *";
            color: red;
        }
        .btn-nouveau { background-color: #27ae60; color: white; }
        .btn-modifier { background-color: #3498db; color: white; }
        .btn-supprimer { background-color: #e74c3c; color: white; }
        .btn-enregistrer { background-color: #2ecc71; color: white; }
        .btn-annuler { background-color: #95a5a6; color: white; }
        .btn-pdf { background-color: #e74c3c; color: white; }
        .btn-excel { background-color: #27ae60; color: white; }
        .recherche-container {
            margin-bottom: 15px;
        }
        .total-section {
            background-color: #ecf0f1;
            padding: 15px;
            border-radius: 6px;
            margin-top: 15px;
        }
        .montant-total {
            font-size: 1.5em;
            font-weight: bold;
            color: #2c3e50;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

        <div class="container-fluid">
            <div class="row">
                <div class="col-12">
                    <h1 class="text-center mt-3 mb-3">
                        <i class="fas fa-file-invoice"></i> Gestion des Devis
                    </h1>

                    <!-- Compteur de devis -->
                    <div class="compteur-devis text-center">
                        <asp:Label ID="lblCompteurDevis" runat="server" Text="📋 Devis : 0 total | 💰 Montant : 0 FCFA"></asp:Label>
                    </div>
                </div>
            </div>

            <asp:UpdatePanel ID="UpdatePanelMain" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <div class="row">
                        <!-- Section gauche : Liste des devis -->
                        <div class="col-md-5">
                            <div class="main-container">
                                <h3 class="section-title">
                                    <i class="fas fa-list"></i> Liste des Devis
                                </h3>

                                <!-- Barre de recherche et filtres -->
                                <div class="recherche-container">
                                    <div class="input-group mb-2">
                                        <span class="input-group-text"><i class="fas fa-search"></i></span>
                                        <asp:TextBox ID="txtRechercheDevis" runat="server" CssClass="form-control"
                                                     placeholder="Rechercher un devis..." AutoPostBack="true"></asp:TextBox>
                                    </div>

                                    <div class="d-flex gap-2 mb-2">
                                        <asp:DropDownList ID="ddlFiltreStatut" runat="server" CssClass="form-select" AutoPostBack="true">
                                            <asp:ListItem Text="Tous les statuts" Value=""></asp:ListItem>
                                            <asp:ListItem Text="Brouillon" Value="Brouillon"></asp:ListItem>
                                            <asp:ListItem Text="Envoyé" Value="Envoyé"></asp:ListItem>
                                            <asp:ListItem Text="Déposé" Value="Déposé"></asp:ListItem>
                                            <asp:ListItem Text="Accepté" Value="Accepté"></asp:ListItem>
                                            <asp:ListItem Text="Refusé" Value="Refusé"></asp:ListItem>
                                            <asp:ListItem Text="Révision" Value="Révision"></asp:ListItem>
                                            <asp:ListItem Text="Commandé" Value="Commandé"></asp:ListItem>
                                            <asp:ListItem Text="Facturé" Value="Facturé"></asp:ListItem>
                                        </asp:DropDownList>

                                        <asp:Button ID="btnReinitialiserFiltre" runat="server" Text="Réinitialiser"
                                                    CssClass="btn btn-secondary" />
                                    </div>

                                    <asp:Label ID="lblTotalFiltre" runat="server" CssClass="text-muted small"></asp:Label>
                                </div>

                                <!-- Liste des devis -->
                                <div class="grid-container">
                                    <asp:GridView ID="dgvListeDevis" runat="server" CssClass="table table-striped table-hover"
                                                  AutoGenerateColumns="False" DataKeyNames="DevisID" AllowPaging="True"
                                                  PageSize="10" EmptyDataText="Aucun devis trouvé">
                                        <Columns>
                                            <asp:BoundField DataField="NumeroDevis" HeaderText="N° Devis" />
                                            <asp:BoundField DataField="NomProjet" HeaderText="Projet" />
                                            <asp:BoundField DataField="DateDevis" HeaderText="Date" DataFormatString="{0:dd/MM/yyyy}" />
                                            <asp:TemplateField HeaderText="Statut">
                                                <ItemTemplate>
                                                    <span class='<%# "statut-badge statut-" + Eval("StatutDevis").ToString().ToLower().Replace("é","e").Replace("ô","o") %>'>
                                                        <%# Eval("StatutDevis") %>
                                                    </span>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="MontantHT" HeaderText="Montant HT" DataFormatString="{0:N0} FCFA" />
                                        </Columns>
                                        <PagerSettings Mode="NumericFirstLast" PageButtonCount="5" />
                                    </asp:GridView>
                                </div>

                                <!-- Boutons d'action sur la liste -->
                                <div class="mt-3 text-center">
                                    <asp:Button ID="btnNouveauDevis" runat="server" Text="➕ Nouveau" CssClass="btn btn-nouveau btn-action" />
                                    <asp:Button ID="btnModifierDevis" runat="server" Text="✏️ Modifier" CssClass="btn btn-modifier btn-action" />
                                    <asp:Button ID="btnSupprimerDevis" runat="server" Text="🗑️ Supprimer" CssClass="btn btn-supprimer btn-action"
                                                OnClientClick="return confirm('Êtes-vous sûr de vouloir supprimer ce devis ?');" />
                                    <asp:Button ID="btnExporterExcel" runat="server" Text="📊 Excel" CssClass="btn btn-excel btn-action" />
                                    <asp:Button ID="btnStatistiques" runat="server" Text="📈 Statistiques" CssClass="btn btn-info btn-action" />
                                </div>
                            </div>
                        </div>

                        <!-- Section droite : Détails du devis -->
                        <div class="col-md-7">
                            <div class="main-container">
                                <h3 class="section-title">
                                    <i class="fas fa-edit"></i> Détails du Devis
                                </h3>

                                <!-- Informations générales -->
                                <div class="form-section">
                                    <h5><i class="fas fa-info-circle"></i> Informations Générales</h5>
                                    <div class="row">
                                        <div class="col-md-6 mb-3">
                                            <label class="form-label required-field">Numéro Devis</label>
                                            <asp:TextBox ID="txtNumeroDevis" runat="server" CssClass="form-control" ReadOnly="true"></asp:TextBox>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="form-label">Numéro Chrono</label>
                                            <asp:TextBox ID="txtNumeroChrono" runat="server" CssClass="form-control" ReadOnly="true"></asp:TextBox>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="form-label required-field">Projet</label>
                                            <asp:DropDownList ID="cmbProjetDevis" runat="server" CssClass="form-select" AutoPostBack="true"></asp:DropDownList>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="form-label required-field">Date Devis</label>
                                            <asp:TextBox ID="dtpDateDevis" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="form-label required-field">Statut</label>
                                            <asp:DropDownList ID="cmbStatutDevis" runat="server" CssClass="form-select"></asp:DropDownList>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="form-label">Numéro Appel</label>
                                            <asp:TextBox ID="txtNumeroAppel" runat="server" CssClass="form-control" ReadOnly="true"></asp:TextBox>
                                        </div>
                                        <div class="col-12 mb-3">
                                            <label class="form-label required-field">Objet du Devis</label>
                                            <asp:TextBox ID="txtObjetDevis" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <!-- Lignes du devis -->
                                <div class="form-section">
                                    <h5><i class="fas fa-table"></i> Lignes du Devis</h5>

                                    <!-- Ajout de ligne -->
                                    <div class="row mb-3">
                                        <div class="col-md-4">
                                            <label class="form-label">Désignation</label>
                                            <asp:DropDownList ID="cmbDesignationLigneDevis" runat="server" CssClass="form-select" AutoPostBack="true"></asp:DropDownList>
                                        </div>
                                        <div class="col-md-2">
                                            <label class="form-label">Unité</label>
                                            <asp:TextBox ID="txtUniteLigne" runat="server" CssClass="form-control"></asp:TextBox>
                                        </div>
                                        <div class="col-md-2">
                                            <label class="form-label">Quantité</label>
                                            <asp:TextBox ID="txtQuantiteLigne" runat="server" CssClass="form-control" TextMode="Number"></asp:TextBox>
                                        </div>
                                        <div class="col-md-2">
                                            <label class="form-label">Prix Unitaire</label>
                                            <asp:TextBox ID="txtPrixUnitaireLigne" runat="server" CssClass="form-control" TextMode="Number"></asp:TextBox>
                                        </div>
                                        <div class="col-md-2 d-flex align-items-end">
                                            <asp:Button ID="btnAjouterLigne" runat="server" Text="➕ Ajouter" CssClass="btn btn-success w-100" />
                                        </div>
                                    </div>

                                    <!-- Liste des lignes -->
                                    <asp:GridView ID="dgvLignesDevis" runat="server" CssClass="table table-sm table-bordered"
                                                  AutoGenerateColumns="False" EmptyDataText="Aucune ligne ajoutée">
                                        <Columns>
                                            <asp:BoundField DataField="Designation" HeaderText="Désignation" />
                                            <asp:BoundField DataField="Unite" HeaderText="Unité" />
                                            <asp:BoundField DataField="Quantite" HeaderText="Qté" DataFormatString="{0:N2}" />
                                            <asp:BoundField DataField="PrixUnitaire" HeaderText="P.U." DataFormatString="{0:N0}" />
                                            <asp:BoundField DataField="MontantLigne" HeaderText="Montant" DataFormatString="{0:N0}" />
                                            <asp:TemplateField HeaderText="Type">
                                                <ItemTemplate>
                                                    <%# If(Eval("TypeLigne").ToString() = "SECTION", "📁 Section", If(Eval("TypeLigne").ToString() = "TITRE", "📌 Titre", "📄 Ligne")) %>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="SectionNom" HeaderText="Section" />
                                            <asp:TemplateField HeaderText="Actions">
                                                <ItemTemplate>
                                                    <asp:Button ID="btnSupprimerLigne" runat="server" Text="🗑️" CssClass="btn btn-sm btn-danger"
                                                                CommandName="SupprimerLigne" CommandArgument='<%# Container.DataItemIndex %>' />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>

                                    <!-- Boutons sections et titre -->
                                    <div class="mt-2">
                                        <asp:TextBox ID="txtNomSection" runat="server" CssClass="form-control d-inline-block"
                                                     style="width: 200px;" placeholder="Nom de la section"></asp:TextBox>
                                        <asp:Button ID="btnCreerSection" runat="server" Text="📁 Créer Section" CssClass="btn btn-primary btn-sm ms-2" />
                                        <asp:Button ID="btnModifierSection" runat="server" Text="✏️ Modifier Section" CssClass="btn btn-warning btn-sm ms-2" />
                                        <asp:Button ID="btnSupprimerSection" runat="server" Text="🗑️ Supprimer Section" CssClass="btn btn-danger btn-sm ms-2" />
                                        <asp:Button ID="btnAjouterTitre" runat="server" Text="📌 Ajouter Titre" CssClass="btn btn-info btn-sm ms-2" />
                                    </div>
                                </div>

                                <!-- Totaux -->
                                <div class="total-section">
                                    <div class="row">
                                        <div class="col-md-6">
                                            <label class="form-label">Montant HT</label>
                                            <asp:TextBox ID="txtMontantHT" runat="server" CssClass="form-control montant-total" ReadOnly="true"></asp:TextBox>
                                        </div>
                                        <div class="col-md-6">
                                            <label class="form-label">Montant en Lettres</label>
                                            <asp:TextBox ID="txtMontantEnLettres" runat="server" CssClass="form-control" ReadOnly="true" TextMode="MultiLine" Rows="2"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <!-- Boutons d'action -->
                                <div class="mt-3 text-center">
                                    <asp:Button ID="btnEnregistrerDevis" runat="server" Text="💾 Enregistrer" CssClass="btn btn-enregistrer btn-action" />
                                    <asp:Button ID="btnAnnulerDevis" runat="server" Text="❌ Annuler" CssClass="btn btn-annuler btn-action" />
                                    <asp:Button ID="btnDeposerDevis" runat="server" Text="📤 Déposer" CssClass="btn btn-warning btn-action" />
                                    <asp:Button ID="btnRetourDevis" runat="server" Text="📥 Retour" CssClass="btn btn-info btn-action" />
                                    <asp:Button ID="btnCreerFacture" runat="server" Text="💰 Créer Facture" CssClass="btn btn-success btn-action" />
                                    <asp:Button ID="btnImprimerPDF" runat="server" Text="📄 PDF" CssClass="btn btn-pdf btn-action" />
                                </div>
                            </div>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>

        <!-- Message de notification -->
        <div id="messageContainer" style="position: fixed; top: 20px; right: 20px; z-index: 9999;">
            <asp:Label ID="lblMessage" runat="server" CssClass="alert" Visible="false"></asp:Label>
        </div>
    </form>

    <!-- Bootstrap JS -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>

    <script type="text/javascript">
        // Auto-hide messages after 5 seconds
        setTimeout(function() {
            var msg = document.getElementById('<%= lblMessage.ClientID %>');
            if (msg) {
                msg.style.display = 'none';
            }
        }, 5000);
    </script>
</body>
</html>
