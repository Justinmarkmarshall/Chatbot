{{- define "chatbot.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "chatbot.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name (include "chatbot.name" .) | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}

{{- define "chatbot.labels" -}}
app.kubernetes.io/name: {{ include "chatbot.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" }}
{{- end }}
{{- define "chatbot.databaseEnv" -}}
{{- if .Values.database.existingSecret }}
- name: ConnectionStrings__Chatbot
  valueFrom:
    secretKeyRef:
      name: {{ .Values.database.existingSecret | quote }}
      key: {{ .Values.database.connectionStringKey | quote }}
{{- else }}
- name: Database__Host
  value: {{ required "database.host is required" .Values.database.host | quote }}
- name: Database__Port
  value: {{ .Values.database.port | quote }}
- name: Database__Name
  value: {{ .Values.database.name | quote }}
- name: Database__Username
  value: {{ .Values.database.username | quote }}
- name: Database__Password
  valueFrom:
    secretKeyRef:
      name: {{ required "database.passwordSecret is required" .Values.database.passwordSecret | quote }}
      key: {{ .Values.database.passwordKey | quote }}
{{- end }}
{{- end -}}
