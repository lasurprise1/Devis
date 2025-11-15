' ********************************************************************************
' Fichier : Devis.aspx.vb - TRANSFORMATION WEB DE Form7.vb
' Description : Gestion complète des devis en ASP.NET WebForms
' ********************************************************************************

Imports System.Data
Imports System.Data.SqlClient
Imports iTextSharp.text
Imports iTextSharp.text.pdf
Imports System.IO
Imports System.Globalization
Imports System.Text
Imports System.Web.UI.WebControls

Public Class Devis
    Inherits System.Web.UI.Page

#Region "Variables Globales"
    Private devisID As Integer = 0
    Private TableLignesDevis As New DataTable()
    Private dtDevisComplet As DataTable
    Private dtDevisCompletAvecStats As DataTable
    Private sectionActuelle As Integer = 0
    Private nombreSections As Integer = 0
    Private lignesEnCours As New List(Of Integer)
    Private StatistiquesDevisActuelles As StatistiquesDevis

    ' ViewState Properties pour persister les données entre postbacks
    Private Property CurrentDevisID As Integer
        Get
            If ViewState("CurrentDevisID") IsNot Nothing Then
                Return CInt(ViewState("CurrentDevisID"))
            End If
            Return 0
        End Get
        Set(value As Integer)
            ViewState("CurrentDevisID") = value
        End Set
    End Property

    Private Property IsEditMode As Boolean
        Get
            If ViewState("IsEditMode") IsNot Nothing Then
                Return CBool(ViewState("IsEditMode"))
            End If
            Return False
        End Get
        Set(value As Boolean)
            ViewState("IsEditMode") = value
        End Set
    End Property
#End Region

#Region "Structures"
    ' Structure pour statistiques
    Public Structure StatistiquesDevis
        Public NombreTotal As Integer
        Public NombreBrouillons As Integer
        Public NombreEnvoyes As Integer
        Public NombreDeposes As Integer
        Public NombreAcceptes As Integer
        Public NombreRefuses As Integer
        Public NombreRevision As Integer
        Public NombreCommandes As Integer
        Public NombreFactures As Integer
        Public MontantTotalHT As Decimal
        Public MontantMoyenHT As Decimal
        Public DatePremier As Date
        Public DateDernier As Date
        Public ProjetLePlusActif As String
    End Structure

    ' Structure pour bon de commande
    Public Structure InfoBonCommande
        Public Existe As Boolean
        Public BonCommandeID As Integer
        Public NumeroBon As String
        Public ARefacturer As String
        Public NumeroBonCommande As String
        Public DateBonCommande As Date
    End Structure
#End Region

#Region "Événements de Page"
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Try
                InitialiserTableLignesDevis()
                ChargerComboBoxes()
                ChargerDevis()
                DesactiverControlesEdition(False)
                MettreAJourCompteurDevis()
            Catch ex As Exception
                AfficherMessage("Erreur lors du chargement : " & ex.Message, "danger")
            End Try
        End If
    End Sub
#End Region

#Region "Initialisation"
    Private Sub InitialiserTableLignesDevis()
        TableLignesDevis.Clear()
        TableLignesDevis.Columns.Clear()

        TableLignesDevis.Columns.Add("Designation", GetType(String))
        TableLignesDevis.Columns.Add("Unite", GetType(String))
        TableLignesDevis.Columns.Add("Quantite", GetType(Decimal))
        TableLignesDevis.Columns.Add("PrixUnitaire", GetType(Decimal))
        TableLignesDevis.Columns.Add("MontantLigne", GetType(Decimal))
        TableLignesDevis.Columns.Add("TypeLigne", GetType(String))
        TableLignesDevis.Columns.Add("SectionNom", GetType(String))
        TableLignesDevis.Columns.Add("OrdreAffichage", GetType(Integer))

        ' Lier au GridView
        dgvLignesDevis.DataSource = TableLignesDevis
        dgvLignesDevis.DataBind()
    End Sub

    Private Sub ChargerComboBoxes()
        ' Charger les projets
        Dim dtProjets As DataTable = DbHelper.GetData("SELECT ProjetID, NomProjet, NumeroProjet FROM Projets WHERE StatutProjet <> 'Terminé' ORDER BY NomProjet")
        cmbProjetDevis.DataSource = dtProjets
        cmbProjetDevis.DataTextField = "NomProjet"
        cmbProjetDevis.DataValueField = "ProjetID"
        cmbProjetDevis.DataBind()
        cmbProjetDevis.Items.Insert(0, New ListItem("-- Sélectionner un projet --", "0"))

        ' Charger les tâches prédéfinies
        Dim dtTaches As DataTable = DbHelper.GetData("SELECT Designation, Unite, PrixUnitaire FROM TachesPredefinies ORDER BY Designation")
        cmbDesignationLigneDevis.DataSource = dtTaches
        cmbDesignationLigneDevis.DataTextField = "Designation"
        cmbDesignationLigneDevis.DataValueField = "Designation"
        cmbDesignationLigneDevis.DataBind()
        cmbDesignationLigneDevis.Items.Insert(0, New ListItem("-- Sélectionner une tâche --", ""))

        ' Charger les statuts
        cmbStatutDevis.Items.Clear()
        cmbStatutDevis.Items.Add(New ListItem("Brouillon", "Brouillon"))
        cmbStatutDevis.Items.Add(New ListItem("Envoyé", "Envoyé"))
        cmbStatutDevis.Items.Add(New ListItem("Déposé", "Déposé"))
        cmbStatutDevis.Items.Add(New ListItem("Accepté", "Accepté"))
        cmbStatutDevis.Items.Add(New ListItem("Refusé", "Refusé"))
        cmbStatutDevis.Items.Add(New ListItem("Révision", "Révision"))
        cmbStatutDevis.Items.Add(New ListItem("Commandé", "Commandé"))
        cmbStatutDevis.Items.Add(New ListItem("Facturé", "Facturé"))
        cmbStatutDevis.Items.Add(New ListItem("Annulé", "Annulé"))
    End Sub
#End Region

#Region "Chargement des Données"
    Private Sub ChargerDevis()
        Try
            Dim query As String = "
                SELECT
                    d.DevisID,
                    d.NumeroDevis,
                    d.NumeroChrono,
                    d.ProjetID,
                    p.NomProjet,
                    p.NumeroProjet,
                    d.DateDevis,
                    d.StatutDevis,
                    d.MontantHT,
                    d.ObjetDevis
                FROM DEVIS d
                LEFT JOIN Projets p ON d.ProjetID = p.ProjetID
                ORDER BY d.DateDevis DESC, d.NumeroChrono DESC"

            dtDevisComplet = DbHelper.GetData(query)

            dgvListeDevis.DataSource = dtDevisComplet
            dgvListeDevis.DataBind()

            MettreAJourCompteurDevis()
        Catch ex As Exception
            AfficherMessage("Erreur lors du chargement des devis : " & ex.Message, "danger")
        End Try
    End Sub

    Private Sub ChargerLignesDevis(devisIDParam As Integer)
        Try
            Dim query As String = "
                SELECT
                    Designation,
                    Unite,
                    Quantite,
                    PrixUnitaire,
                    MontantLigne,
                    TypeLigne,
                    SectionNom,
                    OrdreAffichage
                FROM LIGNESDEVIS
                WHERE DevisID = @DevisID
                ORDER BY OrdreAffichage"

            Dim params As New Dictionary(Of String, Object) From {
                {"@DevisID", devisIDParam}
            }

            TableLignesDevis = DbHelper.GetData(query, params)
            dgvLignesDevis.DataSource = TableLignesDevis
            dgvLignesDevis.DataBind()

            CalculerTotauxDevis()
        Catch ex As Exception
            AfficherMessage("Erreur lors du chargement des lignes : " & ex.Message, "danger")
        End Try
    End Sub
#End Region

#Region "Événements - Liste des Devis"
    Protected Sub dgvListeDevis_SelectedIndexChanged(sender As Object, e As EventArgs) Handles dgvListeDevis.SelectedIndexChanged
        Try
            If dgvListeDevis.SelectedRow IsNot Nothing Then
                Dim selectedDevisID As Integer = CInt(dgvListeDevis.SelectedDataKey.Value)
                AfficherDetailsDevis(selectedDevisID)
            End If
        Catch ex As Exception
            AfficherMessage("Erreur lors de la sélection : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub dgvListeDevis_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles dgvListeDevis.RowCommand
        If e.CommandName = "Select" Then
            Dim index As Integer = Convert.ToInt32(e.CommandArgument)
            Dim row As GridViewRow = dgvListeDevis.Rows(index)
            Dim selectedDevisID As Integer = CInt(dgvListeDevis.DataKeys(index).Value)
            AfficherDetailsDevis(selectedDevisID)
        End If
    End Sub

    Private Sub AfficherDetailsDevis(devisIDParam As Integer)
        Try
            Dim query As String = "SELECT * FROM DEVIS WHERE DevisID = @DevisID"
            Dim params As New Dictionary(Of String, Object) From {{"@DevisID", devisIDParam}}
            Dim dt As DataTable = DbHelper.GetData(query, params)

            If dt.Rows.Count > 0 Then
                Dim row As DataRow = dt.Rows(0)

                txtNumeroDevis.Text = row("NumeroDevis").ToString()
                txtNumeroChrono.Text = row("NumeroChrono").ToString()
                cmbProjetDevis.SelectedValue = row("ProjetID").ToString()
                dtpDateDevis.Text = Convert.ToDateTime(row("DateDevis")).ToString("yyyy-MM-dd")
                cmbStatutDevis.SelectedValue = row("StatutDevis").ToString()
                txtObjetDevis.Text = row("ObjetDevis").ToString()
                txtMontantHT.Text = Convert.ToDecimal(row("MontantHT")).ToString("N0") & " FCFA"

                CurrentDevisID = devisIDParam
                ChargerLignesDevis(devisIDParam)
                RecupererInfosProjet()

                DesactiverControlesEdition(False)
                GererBoutonsSelon Statut(row("StatutDevis").ToString())
            End If
        Catch ex As Exception
            AfficherMessage("Erreur lors de l'affichage : " & ex.Message, "danger")
        End Try
    End Sub
#End Region

#Region "Événements - Boutons CRUD"
    Protected Sub btnNouveauDevis_Click(sender As Object, e As EventArgs) Handles btnNouveauDevis.Click
        Try
            ViderChamps()
            IsEditMode = True
            CurrentDevisID = 0

            ' Générer nouveau numéro
            GenererNumeroDevis()

            ' Définir date du jour
            dtpDateDevis.Text = DateTime.Now.ToString("yyyy-MM-dd")

            ' Statut par défaut
            cmbStatutDevis.SelectedValue = "Brouillon"

            DesactiverControlesEdition(True)
            AfficherMessage("Mode création activé. Remplissez les champs et cliquez sur Enregistrer.", "info")
        Catch ex As Exception
            AfficherMessage("Erreur : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub btnModifierDevis_Click(sender As Object, e As EventArgs) Handles btnModifierDevis.Click
        Try
            If CurrentDevisID = 0 Then
                AfficherMessage("Veuillez sélectionner un devis à modifier.", "warning")
                Return
            End If

            ' Vérifier si le devis peut être modifié
            If VerifierDevisModifiable(CurrentDevisID) Then
                IsEditMode = True
                DesactiverControlesEdition(True)
                AfficherMessage("Mode modification activé. Modifiez les champs et cliquez sur Enregistrer.", "info")
            Else
                AfficherMessage("Ce devis ne peut pas être modifié (statut : Facturé ou Commandé).", "warning")
            End If
        Catch ex As Exception
            AfficherMessage("Erreur : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub btnEnregistrerDevis_Click(sender As Object, e As EventArgs) Handles btnEnregistrerDevis.Click
        Try
            If Not ValiderChamps() Then
                Return
            End If

            Dim projetID As Integer = CInt(cmbProjetDevis.SelectedValue)
            Dim numeroDevis As String = txtNumeroDevis.Text.Trim()
            Dim numeroChrono As String = txtNumeroChrono.Text.Trim()
            Dim dateDevis As Date = Convert.ToDateTime(dtpDateDevis.Text)
            Dim statutDevis As String = cmbStatutDevis.SelectedValue
            Dim objetDevis As String = txtObjetDevis.Text.Trim()
            Dim montantHT As Decimal = CalculerMontantTotal()

            If CurrentDevisID = 0 Then
                ' Insertion
                Dim query As String = "
                    INSERT INTO DEVIS (NumeroDevis, NumeroChrono, ProjetID, DateDevis, StatutDevis, ObjetDevis, MontantHT)
                    VALUES (@NumeroDevis, @NumeroChrono, @ProjetID, @DateDevis, @StatutDevis, @ObjetDevis, @MontantHT);
                    SELECT SCOPE_IDENTITY();"

                Dim params As New Dictionary(Of String, Object) From {
                    {"@NumeroDevis", numeroDevis},
                    {"@NumeroChrono", numeroChrono},
                    {"@ProjetID", projetID},
                    {"@DateDevis", dateDevis},
                    {"@StatutDevis", statutDevis},
                    {"@ObjetDevis", objetDevis},
                    {"@MontantHT", montantHT}
                }

                CurrentDevisID = Convert.ToInt32(DbHelper.ExecuteScalar(query, params))
                EnregistrerLignesDevis()

                AfficherMessage("Devis créé avec succès !", "success")
            Else
                ' Mise à jour
                Dim query As String = "
                    UPDATE DEVIS
                    SET ProjetID = @ProjetID,
                        DateDevis = @DateDevis,
                        StatutDevis = @StatutDevis,
                        ObjetDevis = @ObjetDevis,
                        MontantHT = @MontantHT
                    WHERE DevisID = @DevisID"

                Dim params As New Dictionary(Of String, Object) From {
                    {"@DevisID", CurrentDevisID},
                    {"@ProjetID", projetID},
                    {"@DateDevis", dateDevis},
                    {"@StatutDevis", statutDevis},
                    {"@ObjetDevis", objetDevis},
                    {"@MontantHT", montantHT}
                }

                DbHelper.ExecuteNonQuery(query, params)
                EnregistrerLignesDevis()

                AfficherMessage("Devis modifié avec succès !", "success")
            End If

            IsEditMode = False
            DesactiverControlesEdition(False)
            ChargerDevis()
        Catch ex As Exception
            AfficherMessage("Erreur lors de l'enregistrement : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub btnSupprimerDevis_Click(sender As Object, e As EventArgs) Handles btnSupprimerDevis.Click
        Try
            If CurrentDevisID = 0 Then
                AfficherMessage("Veuillez sélectionner un devis à supprimer.", "warning")
                Return
            End If

            ' Supprimer les lignes d'abord
            DbHelper.ExecuteNonQuery("DELETE FROM LIGNESDEVIS WHERE DevisID = @DevisID",
                New Dictionary(Of String, Object) From {{"@DevisID", CurrentDevisID}})

            ' Supprimer le devis
            DbHelper.ExecuteNonQuery("DELETE FROM DEVIS WHERE DevisID = @DevisID",
                New Dictionary(Of String, Object) From {{"@DevisID", CurrentDevisID}})

            AfficherMessage("Devis supprimé avec succès !", "success")
            ViderChamps()
            ChargerDevis()
        Catch ex As Exception
            AfficherMessage("Erreur lors de la suppression : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub btnAnnulerDevis_Click(sender As Object, e As EventArgs) Handles btnAnnulerDevis.Click
        Try
            IsEditMode = False
            DesactiverControlesEdition(False)

            If CurrentDevisID > 0 Then
                AfficherDetailsDevis(CurrentDevisID)
            Else
                ViderChamps()
            End If

            AfficherMessage("Modification annulée.", "info")
        Catch ex As Exception
            AfficherMessage("Erreur : " & ex.Message, "danger")
        End Try
    End Sub
#End Region

#Region "Gestion des Lignes"
    Protected Sub btnAjouterLigne_Click(sender As Object, e As EventArgs) Handles btnAjouterLigne.Click
        Try
            If Not ValiderSaisieLigne() Then
                Return
            End If

            Dim designation As String = cmbDesignationLigneDevis.SelectedValue
            Dim unite As String = txtUniteLigne.Text.Trim()
            Dim quantite As Decimal = Convert.ToDecimal(txtQuantiteLigne.Text)
            Dim prixUnitaire As Decimal = Convert.ToDecimal(txtPrixUnitaireLigne.Text)
            Dim montantLigne As Decimal = quantite * prixUnitaire

            Dim newRow As DataRow = TableLignesDevis.NewRow()
            newRow("Designation") = designation
            newRow("Unite") = unite
            newRow("Quantite") = quantite
            newRow("PrixUnitaire") = prixUnitaire
            newRow("MontantLigne") = montantLigne
            newRow("TypeLigne") = "LIGNE"
            newRow("SectionNom") = DBNull.Value
            newRow("OrdreAffichage") = TableLignesDevis.Rows.Count + 1

            TableLignesDevis.Rows.Add(newRow)

            dgvLignesDevis.DataSource = TableLignesDevis
            dgvLignesDevis.DataBind()

            CalculerTotauxDevis()
            ViderChampsLigne()

            AfficherMessage("Ligne ajoutée avec succès !", "success")
        Catch ex As Exception
            AfficherMessage("Erreur lors de l'ajout de la ligne : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub dgvLignesDevis_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles dgvLignesDevis.RowCommand
        If e.CommandName = "SupprimerLigne" Then
            Try
                Dim index As Integer = Convert.ToInt32(e.CommandArgument)
                TableLignesDevis.Rows(index).Delete()
                TableLignesDevis.AcceptChanges()

                dgvLignesDevis.DataSource = TableLignesDevis
                dgvLignesDevis.DataBind()

                CalculerTotauxDevis()
                AfficherMessage("Ligne supprimée avec succès !", "success")
            Catch ex As Exception
                AfficherMessage("Erreur lors de la suppression : " & ex.Message, "danger")
            End Try
        End If
    End Sub

    Protected Sub cmbDesignationLigneDevis_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbDesignationLigneDevis.SelectedIndexChanged
        Try
            If cmbDesignationLigneDevis.SelectedIndex > 0 Then
                Dim designation As String = cmbDesignationLigneDevis.SelectedValue
                Dim query As String = "SELECT Unite, PrixUnitaire FROM TachesPredefinies WHERE Designation = @Designation"
                Dim params As New Dictionary(Of String, Object) From {{"@Designation", designation}}
                Dim dt As DataTable = DbHelper.GetData(query, params)

                If dt.Rows.Count > 0 Then
                    txtUniteLigne.Text = dt.Rows(0)("Unite").ToString()
                    txtPrixUnitaireLigne.Text = dt.Rows(0)("PrixUnitaire").ToString()
                End If
            End If
        Catch ex As Exception
            AfficherMessage("Erreur : " & ex.Message, "danger")
        End Try
    End Sub

    Private Sub EnregistrerLignesDevis()
        Try
            ' Supprimer les anciennes lignes
            DbHelper.ExecuteNonQuery("DELETE FROM LIGNESDEVIS WHERE DevisID = @DevisID",
                New Dictionary(Of String, Object) From {{"@DevisID", CurrentDevisID}})

            ' Insérer les nouvelles lignes
            Dim query As String = "
                INSERT INTO LIGNESDEVIS (DevisID, Designation, Unite, Quantite, PrixUnitaire, MontantLigne, TypeLigne, SectionNom, OrdreAffichage)
                VALUES (@DevisID, @Designation, @Unite, @Quantite, @PrixUnitaire, @MontantLigne, @TypeLigne, @SectionNom, @OrdreAffichage)"

            For i As Integer = 0 To TableLignesDevis.Rows.Count - 1
                Dim row As DataRow = TableLignesDevis.Rows(i)

                Dim params As New Dictionary(Of String, Object) From {
                    {"@DevisID", CurrentDevisID},
                    {"@Designation", row("Designation")},
                    {"@Unite", If(IsDBNull(row("Unite")), "", row("Unite"))},
                    {"@Quantite", row("Quantite")},
                    {"@PrixUnitaire", row("PrixUnitaire")},
                    {"@MontantLigne", row("MontantLigne")},
                    {"@TypeLigne", row("TypeLigne")},
                    {"@SectionNom", If(IsDBNull(row("SectionNom")), DBNull.Value, row("SectionNom"))},
                    {"@OrdreAffichage", i + 1}
                }

                DbHelper.ExecuteNonQuery(query, params)
            Next
        Catch ex As Exception
            Throw New Exception("Erreur lors de l'enregistrement des lignes : " & ex.Message)
        End Try
    End Sub
#End Region

#Region "Gestion des Sections"
    Protected Sub btnCreerSection_Click(sender As Object, e As EventArgs) Handles btnCreerSection.Click
        Try
            Dim nomSection As String = txtNomSection.Text.Trim()

            If String.IsNullOrEmpty(nomSection) Then
                AfficherMessage("Veuillez saisir un nom de section.", "warning")
                Return
            End If

            If TableLignesDevis.Rows.Count = 0 Then
                AfficherMessage("Aucune ligne disponible pour créer une section.", "warning")
                Return
            End If

            ' Calculer le sous-total de toutes les lignes sans section
            Dim sousTotal As Decimal = 0
            For Each row As DataRow In TableLignesDevis.Rows
                If IsDBNull(row("SectionNom")) AndAlso row("TypeLigne").ToString() = "LIGNE" Then
                    sousTotal += Convert.ToDecimal(row("MontantLigne"))
                    row("SectionNom") = nomSection
                End If
            Next

            ' Ajouter la ligne de section
            Dim sectionRow As DataRow = TableLignesDevis.NewRow()
            sectionRow("Designation") = "SOUS-TOTAL : " & nomSection
            sectionRow("Unite") = ""
            sectionRow("Quantite") = 0
            sectionRow("PrixUnitaire") = 0
            sectionRow("MontantLigne") = sousTotal
            sectionRow("TypeLigne") = "SECTION"
            sectionRow("SectionNom") = nomSection
            sectionRow("OrdreAffichage") = TableLignesDevis.Rows.Count + 1

            TableLignesDevis.Rows.Add(sectionRow)

            dgvLignesDevis.DataSource = TableLignesDevis
            dgvLignesDevis.DataBind()

            txtNomSection.Text = ""
            nombreSections += 1

            AfficherMessage("Section créée avec succès !", "success")
        Catch ex As Exception
            AfficherMessage("Erreur lors de la création de la section : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub btnModifierSection_Click(sender As Object, e As EventArgs) Handles btnModifierSection.Click
        AfficherMessage("Fonctionnalité de modification de section en cours de développement.", "info")
    End Sub

    Protected Sub btnSupprimerSection_Click(sender As Object, e As EventArgs) Handles btnSupprimerSection.Click
        Try
            Dim nomSection As String = txtNomSection.Text.Trim()

            If String.IsNullOrEmpty(nomSection) Then
                AfficherMessage("Veuillez saisir le nom de la section à supprimer.", "warning")
                Return
            End If

            ' Supprimer la ligne de section et libérer les lignes
            Dim rowsToDelete As New List(Of DataRow)

            For Each row As DataRow In TableLignesDevis.Rows
                If Not IsDBNull(row("SectionNom")) AndAlso row("SectionNom").ToString() = nomSection Then
                    If row("TypeLigne").ToString() = "SECTION" Then
                        rowsToDelete.Add(row)
                    Else
                        row("SectionNom") = DBNull.Value
                    End If
                End If
            Next

            For Each row As DataRow In rowsToDelete
                TableLignesDevis.Rows.Remove(row)
            Next

            TableLignesDevis.AcceptChanges()
            dgvLignesDevis.DataSource = TableLignesDevis
            dgvLignesDevis.DataBind()

            txtNomSection.Text = ""
            CalculerTotauxDevis()

            AfficherMessage("Section supprimée avec succès !", "success")
        Catch ex As Exception
            AfficherMessage("Erreur lors de la suppression de la section : " & ex.Message, "danger")
        End Try
    End Sub

    Protected Sub btnAjouterTitre_Click(sender As Object, e As EventArgs) Handles btnAjouterTitre.Click
        Try
            Dim nomTitre As String = txtNomSection.Text.Trim()

            If String.IsNullOrEmpty(nomTitre) Then
                AfficherMessage("Veuillez saisir le texte du titre.", "warning")
                Return
            End If

            Dim titreRow As DataRow = TableLignesDevis.NewRow()
            titreRow("Designation") = nomTitre
            titreRow("Unite") = ""
            titreRow("Quantite") = 0
            titreRow("PrixUnitaire") = 0
            titreRow("MontantLigne") = 0
            titreRow("TypeLigne") = "TITRE"
            titreRow("SectionNom") = DBNull.Value
            titreRow("OrdreAffichage") = TableLignesDevis.Rows.Count + 1

            TableLignesDevis.Rows.Add(titreRow)

            dgvLignesDevis.DataSource = TableLignesDevis
            dgvLignesDevis.DataBind()

            txtNomSection.Text = ""

            AfficherMessage("Titre ajouté avec succès !", "success")
        Catch ex As Exception
            AfficherMessage("Erreur lors de l'ajout du titre : " & ex.Message, "danger")
        End Try
    End Sub
#End Region

#Region "Génération PDF"
    Protected Sub btnImprimerPDF_Click(sender As Object, e As EventArgs) Handles btnImprimerPDF.Click
        Try
            If CurrentDevisID = 0 Then
                AfficherMessage("Veuillez sélectionner un devis à imprimer.", "warning")
                Return
            End If

            ' Générer le PDF
            Dim pdfBytes() As Byte = GenererDevisPDF()

            ' Envoyer le PDF au navigateur
            Response.Clear()
            Response.ContentType = "application/pdf"
            Response.AddHeader("Content-Disposition", "attachment; filename=Devis_" & txtNumeroDevis.Text.Replace("/", "_") & ".pdf")
            Response.BinaryWrite(pdfBytes)
            Response.End()

        Catch ex As Exception
            AfficherMessage("Erreur lors de la génération du PDF : " & ex.Message, "danger")
        End Try
    End Sub

    Private Function GenererDevisPDF() As Byte()
        Dim memoryStream As New MemoryStream()

        Try
            ' Créer le document PDF
            Dim document As New Document(PageSize.A4, 50, 50, 100, 80)
            Dim writer As PdfWriter = PdfWriter.GetInstance(document, memoryStream)

            document.Open()

            ' Polices
            Dim fontTitre As New Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, BaseColor.BLACK)
            Dim fontNormal As New Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK)
            Dim fontBold As New Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.BLACK)

            ' En-tête
            Dim titre As New Paragraph("DEVIS", fontTitre)
            titre.Alignment = Element.ALIGN_CENTER
            document.Add(titre)

            document.Add(New Paragraph(" "))

            ' Informations du devis
            document.Add(New Paragraph("Numéro de devis : " & txtNumeroDevis.Text, fontBold))
            document.Add(New Paragraph("Date : " & Convert.ToDateTime(dtpDateDevis.Text).ToString("dd/MM/yyyy"), fontNormal))
            document.Add(New Paragraph("Statut : " & cmbStatutDevis.SelectedValue, fontNormal))
            document.Add(New Paragraph("Objet : " & txtObjetDevis.Text, fontNormal))

            document.Add(New Paragraph(" "))

            ' Informations client
            Dim infosClient As Dictionary(Of String, String) = ObtenirInformationsClient()
            If infosClient IsNot Nothing Then
                document.Add(New Paragraph("Client : " & infosClient("NomClient"), fontBold))
                document.Add(New Paragraph("Adresse : " & infosClient("Adresse"), fontNormal))
                document.Add(New Paragraph("Téléphone : " & infosClient("Telephone"), fontNormal))
                document.Add(New Paragraph("Email : " & infosClient("Email"), fontNormal))
            End If

            document.Add(New Paragraph(" "))

            ' Tableau des lignes
            Dim table As New PdfPTable(5)
            table.WidthPercentage = 100
            table.SetWidths(New Single() {40, 10, 10, 15, 15})

            ' En-têtes
            AjouterCelluleTableau(table, "Désignation", fontBold, Element.ALIGN_CENTER, BaseColor.LIGHT_GRAY)
            AjouterCelluleTableau(table, "Unité", fontBold, Element.ALIGN_CENTER, BaseColor.LIGHT_GRAY)
            AjouterCelluleTableau(table, "Quantité", fontBold, Element.ALIGN_CENTER, BaseColor.LIGHT_GRAY)
            AjouterCelluleTableau(table, "P.U.", fontBold, Element.ALIGN_CENTER, BaseColor.LIGHT_GRAY)
            AjouterCelluleTableau(table, "Montant", fontBold, Element.ALIGN_CENTER, BaseColor.LIGHT_GRAY)

            ' Lignes
            For Each row As DataRow In TableLignesDevis.Rows
                Dim typeLigne As String = row("TypeLigne").ToString()

                If typeLigne = "TITRE" Then
                    ' Titre
                    Dim cellTitre As New PdfPCell(New Phrase(row("Designation").ToString(), fontBold))
                    cellTitre.Colspan = 5
                    cellTitre.BackgroundColor = BaseColor.YELLOW
                    cellTitre.HorizontalAlignment = Element.ALIGN_CENTER
                    cellTitre.Padding = 5
                    table.AddCell(cellTitre)
                ElseIf typeLigne = "SECTION" Then
                    ' Section
                    Dim cellSection As New PdfPCell(New Phrase(row("Designation").ToString(), fontBold))
                    cellSection.Colspan = 4
                    cellSection.BackgroundColor = BaseColor.LIGHT_GRAY
                    cellSection.HorizontalAlignment = Element.ALIGN_RIGHT
                    cellSection.Padding = 5
                    table.AddCell(cellSection)

                    AjouterCelluleTableau(table, Convert.ToDecimal(row("MontantLigne")).ToString("N0") & " FCFA", fontBold, Element.ALIGN_RIGHT, BaseColor.LIGHT_GRAY)
                Else
                    ' Ligne normale
                    AjouterCelluleTableau(table, row("Designation").ToString(), fontNormal, Element.ALIGN_LEFT, BaseColor.WHITE)
                    AjouterCelluleTableau(table, row("Unite").ToString(), fontNormal, Element.ALIGN_CENTER, BaseColor.WHITE)
                    AjouterCelluleTableau(table, Convert.ToDecimal(row("Quantite")).ToString("N2"), fontNormal, Element.ALIGN_CENTER, BaseColor.WHITE)
                    AjouterCelluleTableau(table, Convert.ToDecimal(row("PrixUnitaire")).ToString("N0"), fontNormal, Element.ALIGN_RIGHT, BaseColor.WHITE)
                    AjouterCelluleTableau(table, Convert.ToDecimal(row("MontantLigne")).ToString("N0") & " FCFA", fontNormal, Element.ALIGN_RIGHT, BaseColor.WHITE)
                End If
            Next

            document.Add(table)

            document.Add(New Paragraph(" "))

            ' Total
            Dim montantTotal As Decimal = CalculerMontantTotal()
            Dim paragraphTotal As New Paragraph("TOTAL HT : " & montantTotal.ToString("N0") & " FCFA", fontBold)
            paragraphTotal.Alignment = Element.ALIGN_RIGHT
            document.Add(paragraphTotal)

            ' Montant en lettres
            Dim montantEnLettres As String = ConvertirMontantEnLettres(montantTotal)
            document.Add(New Paragraph("Arrêté le présent devis à la somme de : " & montantEnLettres, fontNormal))

            document.Add(New Paragraph(" "))
            document.Add(New Paragraph(" "))

            ' Signature
            document.Add(New Paragraph("Signature :", fontNormal))
            document.Add(New Paragraph("Véronique KOUAME AKE", fontBold))

            document.Close()

            Return memoryStream.ToArray()

        Catch ex As Exception
            Throw New Exception("Erreur lors de la génération du PDF : " & ex.Message)
        Finally
            memoryStream.Close()
        End Try
    End Function

    Private Sub AjouterCelluleTableau(table As PdfPTable, texte As String, font As Font, alignement As Integer, couleurFond As BaseColor)
        Dim cell As New PdfPCell(New Phrase(texte, font))
        cell.HorizontalAlignment = alignement
        cell.BackgroundColor = couleurFond
        cell.Padding = 5
        table.AddCell(cell)
    End Sub
#End Region

#Region "Recherche et Filtrage"
    Protected Sub txtRechercheDevis_TextChanged(sender As Object, e As EventArgs) Handles txtRechercheDevis.TextChanged
        RechercherDevis(txtRechercheDevis.Text.Trim())
    End Sub

    Protected Sub ddlFiltreStatut_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlFiltreStatut.SelectedIndexChanged
        FiltrerParStatut(ddlFiltreStatut.SelectedValue)
    End Sub

    Protected Sub btnReinitialiserFiltre_Click(sender As Object, e As EventArgs) Handles btnReinitialiserFiltre.Click
        txtRechercheDevis.Text = ""
        ddlFiltreStatut.SelectedIndex = 0
        ChargerDevis()
    End Sub

    Private Sub RechercherDevis(terme As String)
        Try
            If String.IsNullOrEmpty(terme) Then
                dgvListeDevis.DataSource = dtDevisComplet
                dgvListeDevis.DataBind()
                Return
            End If

            Dim dv As DataView = dtDevisComplet.DefaultView
            dv.RowFilter = String.Format("NumeroDevis LIKE '%{0}%' OR NomProjet LIKE '%{0}%' OR StatutDevis LIKE '%{0}%' OR ObjetDevis LIKE '%{0}%'", terme)

            dgvListeDevis.DataSource = dv
            dgvListeDevis.DataBind()

            MettreAJourLabelFiltre(dv.Count)
        Catch ex As Exception
            AfficherMessage("Erreur lors de la recherche : " & ex.Message, "danger")
        End Try
    End Sub

    Private Sub FiltrerParStatut(statut As String)
        Try
            If String.IsNullOrEmpty(statut) Then
                dgvListeDevis.DataSource = dtDevisComplet
                dgvListeDevis.DataBind()
                Return
            End If

            Dim dv As DataView = dtDevisComplet.DefaultView
            dv.RowFilter = String.Format("StatutDevis = '{0}'", statut)

            dgvListeDevis.DataSource = dv
            dgvListeDevis.DataBind()

            MettreAJourLabelFiltre(dv.Count)
        Catch ex As Exception
            AfficherMessage("Erreur lors du filtrage : " & ex.Message, "danger")
        End Try
    End Sub

    Private Sub MettreAJourLabelFiltre(count As Integer)
        lblTotalFiltre.Text = String.Format("{0} devis trouvé(s)", count)
    End Sub
#End Region

#Region "Export Excel"
    Protected Sub btnExporterExcel_Click(sender As Object, e As EventArgs) Handles btnExporterExcel.Click
        Try
            ExporterVersExcel()
        Catch ex As Exception
            AfficherMessage("Erreur lors de l'export : " & ex.Message, "danger")
        End Try
    End Sub

    Private Sub ExporterVersExcel()
        Response.Clear()
        Response.Buffer = True
        Response.AddHeader("content-disposition", "attachment;filename=Devis_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".xls")
        Response.Charset = ""
        Response.ContentType = "application/vnd.ms-excel"

        Dim sw As New StringWriter()
        Dim hw As New HtmlTextWriter(sw)

        dgvListeDevis.RenderControl(hw)

        Response.Output.Write(sw.ToString())
        Response.Flush()
        Response.End()
    End Sub

    Public Overrides Sub VerifyRenderingInServerForm(control As Control)
        ' Required for Excel export
    End Sub
#End Region

#Region "Statistiques"
    Protected Sub btnStatistiques_Click(sender As Object, e As EventArgs) Handles btnStatistiques.Click
        Try
            Dim stats As StatistiquesDevis = ObtenirStatistiques()

            Dim message As String = String.Format(
                "STATISTIQUES DES DEVIS{0}{0}" &
                "Total devis : {1}{0}" &
                "Brouillons : {2}{0}" &
                "Envoyés : {3}{0}" &
                "Déposés : {4}{0}" &
                "Acceptés : {5}{0}" &
                "Refusés : {6}{0}" &
                "En révision : {7}{0}" &
                "Commandés : {8}{0}" &
                "Facturés : {9}{0}{0}" &
                "Montant total HT : {10:N0} FCFA{0}" &
                "Montant moyen HT : {11:N0} FCFA",
                vbCrLf,
                stats.NombreTotal,
                stats.NombreBrouillons,
                stats.NombreEnvoyes,
                stats.NombreDeposes,
                stats.NombreAcceptes,
                stats.NombreRefuses,
                stats.NombreRevision,
                stats.NombreCommandes,
                stats.NombreFactures,
                stats.MontantTotalHT,
                stats.MontantMoyenHT
            )

            ' Afficher dans une alerte JavaScript
            Dim script As String = "alert('" & message.Replace("'", "\'") & "');"
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "ShowStats", script, True)

        Catch ex As Exception
            AfficherMessage("Erreur lors du calcul des statistiques : " & ex.Message, "danger")
        End Try
    End Sub

    Private Function ObtenirStatistiques() As StatistiquesDevis
        Dim stats As New StatistiquesDevis()

        Try
            Dim query As String = "
                SELECT
                    COUNT(*) AS Total,
                    SUM(CASE WHEN StatutDevis = 'Brouillon' THEN 1 ELSE 0 END) AS Brouillons,
                    SUM(CASE WHEN StatutDevis = 'Envoyé' THEN 1 ELSE 0 END) AS Envoyes,
                    SUM(CASE WHEN StatutDevis = 'Déposé' THEN 1 ELSE 0 END) AS Deposes,
                    SUM(CASE WHEN StatutDevis = 'Accepté' THEN 1 ELSE 0 END) AS Acceptes,
                    SUM(CASE WHEN StatutDevis = 'Refusé' THEN 1 ELSE 0 END) AS Refuses,
                    SUM(CASE WHEN StatutDevis = 'Révision' THEN 1 ELSE 0 END) AS Revision,
                    SUM(CASE WHEN StatutDevis = 'Commandé' THEN 1 ELSE 0 END) AS Commandes,
                    SUM(CASE WHEN StatutDevis = 'Facturé' THEN 1 ELSE 0 END) AS Factures,
                    ISNULL(SUM(MontantHT), 0) AS MontantTotal,
                    ISNULL(AVG(MontantHT), 0) AS MontantMoyen
                FROM DEVIS"

            Dim dt As DataTable = DbHelper.GetData(query)

            If dt.Rows.Count > 0 Then
                Dim row As DataRow = dt.Rows(0)
                stats.NombreTotal = Convert.ToInt32(row("Total"))
                stats.NombreBrouillons = Convert.ToInt32(row("Brouillons"))
                stats.NombreEnvoyes = Convert.ToInt32(row("Envoyes"))
                stats.NombreDeposes = Convert.ToInt32(row("Deposes"))
                stats.NombreAcceptes = Convert.ToInt32(row("Acceptes"))
                stats.NombreRefuses = Convert.ToInt32(row("Refuses"))
                stats.NombreRevision = Convert.ToInt32(row("Revision"))
                stats.NombreCommandes = Convert.ToInt32(row("Commandes"))
                stats.NombreFactures = Convert.ToInt32(row("Factures"))
                stats.MontantTotalHT = Convert.ToDecimal(row("MontantTotal"))
                stats.MontantMoyenHT = Convert.ToDecimal(row("MontantMoyen"))
            End If

        Catch ex As Exception
            Throw New Exception("Erreur lors du calcul des statistiques : " & ex.Message)
        End Try

        Return stats
    End Function
#End Region

#Region "Workflow - Dépôt, Retour, Facture"
    Protected Sub btnDeposerDevis_Click(sender As Object, e As EventArgs) Handles btnDeposerDevis.Click
        AfficherMessage("Fonctionnalité de dépôt en cours de développement (nécessite Form6).", "info")
        ' TODO: Ouvrir une modal ou rediriger vers une page de dépôt
    End Sub

    Protected Sub btnRetourDevis_Click(sender As Object, e As EventArgs) Handles btnRetourDevis.Click
        AfficherMessage("Fonctionnalité de retour en cours de développement (nécessite Form18).", "info")
        ' TODO: Ouvrir une modal ou rediriger vers une page de retour
    End Sub

    Protected Sub btnCreerFacture_Click(sender As Object, e As EventArgs) Handles btnCreerFacture.Click
        Try
            If CurrentDevisID = 0 Then
                AfficherMessage("Veuillez sélectionner un devis.", "warning")
                Return
            End If

            ' Vérifier si le devis est accepté ou commandé
            Dim statut As String = cmbStatutDevis.SelectedValue

            If statut <> "Accepté" And statut <> "Commandé" Then
                AfficherMessage("Seuls les devis acceptés ou commandés peuvent être facturés.", "warning")
                Return
            End If

            ' Vérifier si une facture existe déjà
            Dim query As String = "SELECT COUNT(*) FROM Factures WHERE DevisID = @DevisID"
            Dim params As New Dictionary(Of String, Object) From {{"@DevisID", CurrentDevisID}}
            Dim count As Integer = Convert.ToInt32(DbHelper.ExecuteScalar(query, params))

            If count > 0 Then
                AfficherMessage("Une facture existe déjà pour ce devis.", "warning")
                Return
            End If

            AfficherMessage("Fonctionnalité de création de facture en cours de développement.", "info")
            ' TODO: Créer la facture et rediriger vers la page des factures

        Catch ex As Exception
            AfficherMessage("Erreur : " & ex.Message, "danger")
        End Try
    End Sub
#End Region

#Region "Méthodes Utilitaires"
    Private Sub ViderChamps()
        txtNumeroDevis.Text = ""
        txtNumeroChrono.Text = ""
        cmbProjetDevis.SelectedIndex = 0
        dtpDateDevis.Text = ""
        cmbStatutDevis.SelectedIndex = 0
        txtNumeroAppel.Text = ""
        txtObjetDevis.Text = ""
        txtMontantHT.Text = ""
        txtMontantEnLettres.Text = ""

        InitialiserTableLignesDevis()
        CurrentDevisID = 0
    End Sub

    Private Sub ViderChampsLigne()
        cmbDesignationLigneDevis.SelectedIndex = 0
        txtUniteLigne.Text = ""
        txtQuantiteLigne.Text = ""
        txtPrixUnitaireLigne.Text = ""
    End Sub

    Private Sub DesactiverControlesEdition(activer As Boolean)
        cmbProjetDevis.Enabled = activer
        dtpDateDevis.Enabled = activer
        cmbStatutDevis.Enabled = activer
        txtObjetDevis.ReadOnly = Not activer

        cmbDesignationLigneDevis.Enabled = activer
        txtUniteLigne.ReadOnly = Not activer
        txtQuantiteLigne.ReadOnly = Not activer
        txtPrixUnitaireLigne.ReadOnly = Not activer
        btnAjouterLigne.Enabled = activer

        btnCreerSection.Enabled = activer
        btnModifierSection.Enabled = activer
        btnSupprimerSection.Enabled = activer
        btnAjouterTitre.Enabled = activer
        txtNomSection.ReadOnly = Not activer

        btnEnregistrerDevis.Enabled = activer
        btnAnnulerDevis.Enabled = activer
    End Sub

    Private Sub GenererNumeroDevis()
        Try
            ' Récupérer le dernier numéro chrono
            Dim query As String = "SELECT ISNULL(MAX(CAST(NumeroChrono AS INT)), 0) + 1 AS ProchainNumero FROM DEVIS WHERE YEAR(DateDevis) = YEAR(GETDATE())"
            Dim dt As DataTable = DbHelper.GetData(query)

            Dim prochainNumero As Integer = 1
            If dt.Rows.Count > 0 Then
                prochainNumero = Convert.ToInt32(dt.Rows(0)("ProchainNumero"))
            End If

            txtNumeroChrono.Text = prochainNumero.ToString()
            txtNumeroDevis.Text = String.Format("VERNET/KAV-N°{0:000}/{1}", prochainNumero, DateTime.Now.Year)

        Catch ex As Exception
            Throw New Exception("Erreur lors de la génération du numéro : " & ex.Message)
        End Try
    End Sub

    Private Sub CalculerTotauxDevis()
        Try
            Dim total As Decimal = CalculerMontantTotal()

            txtMontantHT.Text = total.ToString("N0") & " FCFA"
            txtMontantEnLettres.Text = ConvertirMontantEnLettres(total)

        Catch ex As Exception
            AfficherMessage("Erreur lors du calcul des totaux : " & ex.Message, "danger")
        End Try
    End Sub

    Private Function CalculerMontantTotal() As Decimal
        Dim total As Decimal = 0

        For Each row As DataRow In TableLignesDevis.Rows
            If row("TypeLigne").ToString() = "LIGNE" Then
                total += Convert.ToDecimal(row("MontantLigne"))
            End If
        Next

        Return total
    End Function

    Private Function ConvertirMontantEnLettres(montant As Decimal) As String
        ' Implémentation simplifiée - À compléter selon les besoins
        Try
            Dim montantEntier As Long = CLng(Math.Floor(montant))
            Dim resultat As String = ""

            If montantEntier = 0 Then
                Return "Zéro Francs CFA"
            End If

            ' Unités
            Dim unites() As String = {"", "Un", "Deux", "Trois", "Quatre", "Cinq", "Six", "Sept", "Huit", "Neuf"}
            Dim dizaines() As String = {"", "Dix", "Vingt", "Trente", "Quarante", "Cinquante", "Soixante", "Soixante-dix", "Quatre-vingt", "Quatre-vingt-dix"}
            Dim dix_a_dix_neuf() As String = {"Dix", "Onze", "Douze", "Treize", "Quatorze", "Quinze", "Seize", "Dix-sept", "Dix-huit", "Dix-neuf"}

            ' Conversion simplifiée pour les millions
            Dim millions As Long = montantEntier \ 1000000
            Dim milliers As Long = (montantEntier Mod 1000000) \ 1000
            Dim centaines As Long = montantEntier Mod 1000

            If millions > 0 Then
                resultat &= ConvertirNombreSimple(millions) & " Million"
                If millions > 1 Then resultat &= "s"
                resultat &= " "
            End If

            If milliers > 0 Then
                resultat &= ConvertirNombreSimple(milliers) & " Mille "
            End If

            If centaines > 0 Then
                resultat &= ConvertirNombreSimple(centaines) & " "
            End If

            resultat &= "Francs CFA"

            Return resultat.Trim()

        Catch ex As Exception
            Return montant.ToString("N0") & " FCFA"
        End Try
    End Function

    Private Function ConvertirNombreSimple(nombre As Long) As String
        If nombre = 0 Then Return ""

        Dim unites() As String = {"", "Un", "Deux", "Trois", "Quatre", "Cinq", "Six", "Sept", "Huit", "Neuf"}
        Dim dizaines() As String = {"", "Dix", "Vingt", "Trente", "Quarante", "Cinquante", "Soixante", "Soixante-dix", "Quatre-vingt", "Quatre-vingt-dix"}
        Dim dix_a_dix_neuf() As String = {"Dix", "Onze", "Douze", "Treize", "Quatorze", "Quinze", "Seize", "Dix-sept", "Dix-huit", "Dix-neuf"}

        Dim resultat As String = ""

        ' Centaines
        Dim c As Integer = CInt(nombre \ 100)
        If c > 1 Then
            resultat &= unites(c) & " Cent "
        ElseIf c = 1 Then
            resultat &= "Cent "
        End If

        ' Dizaines et unités
        Dim reste As Integer = CInt(nombre Mod 100)

        If reste >= 10 And reste <= 19 Then
            resultat &= dix_a_dix_neuf(reste - 10) & " "
        Else
            Dim d As Integer = reste \ 10
            Dim u As Integer = reste Mod 10

            If d > 0 Then
                resultat &= dizaines(d) & " "
            End If

            If u > 0 Then
                resultat &= unites(u) & " "
            End If
        End If

        Return resultat.Trim()
    End Function

    Private Function ObtenirInformationsClient() As Dictionary(Of String, String)
        Try
            If cmbProjetDevis.SelectedIndex <= 0 Then
                Return Nothing
            End If

            Dim projetID As Integer = CInt(cmbProjetDevis.SelectedValue)

            Dim query As String = "
                SELECT
                    c.NomClient,
                    c.Adresse,
                    c.Telephone,
                    c.Email
                FROM Clients c
                INNER JOIN Projets p ON c.ClientID = p.ClientID
                WHERE p.ProjetID = @ProjetID"

            Dim params As New Dictionary(Of String, Object) From {{"@ProjetID", projetID}}
            Dim dt As DataTable = DbHelper.GetData(query, params)

            If dt.Rows.Count > 0 Then
                Dim infos As New Dictionary(Of String, String)
                infos("NomClient") = dt.Rows(0)("NomClient").ToString()
                infos("Adresse") = dt.Rows(0)("Adresse").ToString()
                infos("Telephone") = dt.Rows(0)("Telephone").ToString()
                infos("Email") = dt.Rows(0)("Email").ToString()
                Return infos
            End If

        Catch ex As Exception
            ' Ignorer l'erreur
        End Try

        Return Nothing
    End Function

    Protected Sub cmbProjetDevis_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbProjetDevis.SelectedIndexChanged
        RecupererInfosProjet()
    End Sub

    Private Sub RecupererInfosProjet()
        Try
            If cmbProjetDevis.SelectedIndex <= 0 Then
                Return
            End If

            Dim projetID As Integer = CInt(cmbProjetDevis.SelectedValue)

            ' Récupérer le numéro d'appel
            Dim query As String = "
                SELECT ac.NumeroAppel
                FROM Projets p
                LEFT JOIN AppelClient ac ON p.AppelClientID = ac.AppelClientID
                WHERE p.ProjetID = @ProjetID"

            Dim params As New Dictionary(Of String, Object) From {{"@ProjetID", projetID}}
            Dim dt As DataTable = DbHelper.GetData(query, params)

            If dt.Rows.Count > 0 AndAlso Not IsDBNull(dt.Rows(0)("NumeroAppel")) Then
                txtNumeroAppel.Text = dt.Rows(0)("NumeroAppel").ToString()
            Else
                txtNumeroAppel.Text = ""
            End If

        Catch ex As Exception
            ' Ignorer l'erreur
        End Try
    End Sub

    Private Function ValiderChamps() As Boolean
        If cmbProjetDevis.SelectedIndex <= 0 Then
            AfficherMessage("Veuillez sélectionner un projet.", "warning")
            Return False
        End If

        If String.IsNullOrWhiteSpace(dtpDateDevis.Text) Then
            AfficherMessage("Veuillez saisir une date.", "warning")
            Return False
        End If

        If String.IsNullOrWhiteSpace(txtObjetDevis.Text) Then
            AfficherMessage("Veuillez saisir l'objet du devis.", "warning")
            Return False
        End If

        Return True
    End Function

    Private Function ValiderSaisieLigne() As Boolean
        If cmbDesignationLigneDevis.SelectedIndex <= 0 Then
            AfficherMessage("Veuillez sélectionner une désignation.", "warning")
            Return False
        End If

        If String.IsNullOrWhiteSpace(txtQuantiteLigne.Text) OrElse Convert.ToDecimal(txtQuantiteLigne.Text) <= 0 Then
            AfficherMessage("Veuillez saisir une quantité valide.", "warning")
            Return False
        End If

        If String.IsNullOrWhiteSpace(txtPrixUnitaireLigne.Text) OrElse Convert.ToDecimal(txtPrixUnitaireLigne.Text) <= 0 Then
            AfficherMessage("Veuillez saisir un prix unitaire valide.", "warning")
            Return False
        End If

        Return True
    End Function

    Private Function VerifierDevisModifiable(devisIDParam As Integer) As Boolean
        Try
            Dim query As String = "SELECT StatutDevis FROM DEVIS WHERE DevisID = @DevisID"
            Dim params As New Dictionary(Of String, Object) From {{"@DevisID", devisIDParam}}
            Dim dt As DataTable = DbHelper.GetData(query, params)

            If dt.Rows.Count > 0 Then
                Dim statut As String = dt.Rows(0)("StatutDevis").ToString()
                Return statut <> "Facturé" AndAlso statut <> "Commandé"
            End If

        Catch ex As Exception
            ' En cas d'erreur, on autorise la modification
        End Try

        Return True
    End Function

    Private Sub GererBoutonsSelonStatut(statut As String)
        ' Activer/désactiver les boutons selon le statut
        Select Case statut
            Case "Brouillon", "Envoyé"
                btnDeposerDevis.Enabled = True
                btnRetourDevis.Enabled = False
                btnCreerFacture.Enabled = False

            Case "Déposé"
                btnDeposerDevis.Enabled = False
                btnRetourDevis.Enabled = True
                btnCreerFacture.Enabled = False

            Case "Accepté"
                btnDeposerDevis.Enabled = False
                btnRetourDevis.Enabled = False
                btnCreerFacture.Enabled = True

            Case "Commandé"
                btnDeposerDevis.Enabled = False
                btnRetourDevis.Enabled = False
                btnCreerFacture.Enabled = True

            Case "Facturé"
                btnDeposerDevis.Enabled = False
                btnRetourDevis.Enabled = False
                btnCreerFacture.Enabled = False
                btnModifierDevis.Enabled = False
                btnSupprimerDevis.Enabled = False

            Case Else
                btnDeposerDevis.Enabled = False
                btnRetourDevis.Enabled = False
                btnCreerFacture.Enabled = False
        End Select
    End Sub

    Private Sub MettreAJourCompteurDevis()
        Try
            If dtDevisComplet IsNot Nothing AndAlso dtDevisComplet.Rows.Count > 0 Then
                Dim nombreTotal As Integer = dtDevisComplet.Rows.Count
                Dim montantTotal As Decimal = 0

                For Each row As DataRow In dtDevisComplet.Rows
                    If Not IsDBNull(row("MontantHT")) Then
                        montantTotal += Convert.ToDecimal(row("MontantHT"))
                    End If
                Next

                lblCompteurDevis.Text = String.Format("📋 Devis : {0} total | 💰 Montant : {1:N0} FCFA", nombreTotal, montantTotal)
            Else
                lblCompteurDevis.Text = "📋 Devis : 0 total | 💰 Montant : 0 FCFA"
            End If

        Catch ex As Exception
            ' Ignorer l'erreur
        End Try
    End Sub

    Private Sub AfficherMessage(message As String, type As String)
        lblMessage.Text = message
        lblMessage.CssClass = "alert alert-" & type
        lblMessage.Visible = True
    End Sub
#End Region

#Region "Classe DbHelper (À placer dans un fichier séparé)"
    ' Cette classe doit être placée dans un fichier séparé DbHelper.vb
    ' Voici une version simplifiée pour référence

    Public Class DbHelper
        Private Shared ConnectionString As String = System.Configuration.ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString

        Public Shared Function GetData(query As String, Optional params As Dictionary(Of String, Object) = Nothing) As DataTable
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                Using cmd As New SqlCommand(query, conn)
                    If params IsNot Nothing Then
                        For Each param In params
                            cmd.Parameters.AddWithValue(param.Key, param.Value)
                        Next
                    End If

                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using
                End Using
            End Using

            Return dt
        End Function

        Public Shared Function ExecuteNonQuery(query As String, Optional params As Dictionary(Of String, Object) = Nothing) As Integer
            Dim rowsAffected As Integer = 0

            Using conn As New SqlConnection(ConnectionString)
                Using cmd As New SqlCommand(query, conn)
                    If params IsNot Nothing Then
                        For Each param In params
                            cmd.Parameters.AddWithValue(param.Key, param.Value)
                        Next
                    End If

                    conn.Open()
                    rowsAffected = cmd.ExecuteNonQuery()
                End Using
            End Using

            Return rowsAffected
        End Function

        Public Shared Function ExecuteScalar(query As String, Optional params As Dictionary(Of String, Object) = Nothing) As Object
            Dim result As Object = Nothing

            Using conn As New SqlConnection(ConnectionString)
                Using cmd As New SqlCommand(query, conn)
                    If params IsNot Nothing Then
                        For Each param In params
                            cmd.Parameters.AddWithValue(param.Key, param.Value)
                        Next
                    End If

                    conn.Open()
                    result = cmd.ExecuteScalar()
                End Using
            End Using

            Return result
        End Function
    End Class
#End Region
End Class
