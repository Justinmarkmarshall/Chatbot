{{- define "chatbot-postgres.name" -}}
{{- default .Release.Name .Values.fullnameOverride | trunc 50 | trimSuffix "-" -}}
{{- end -}}
{{- define "chatbot-postgres.selector" -}}
app.kubernetes.io/name: chatbot-postgres
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}
