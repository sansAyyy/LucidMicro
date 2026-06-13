node_prefix "" {
  policy = "read"
}

service_prefix "" {
  policy = "read"
}

service "identity" {
  policy = "write"
}

service "notification" {
  policy = "write"
}
