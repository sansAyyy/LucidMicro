\set identity_db lucid_micro_identity
\set notification_db lucid_micro_notification

SELECT format('CREATE DATABASE %I', :'identity_db')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = :'identity_db')\gexec

SELECT format('CREATE DATABASE %I', :'notification_db')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = :'notification_db')\gexec
