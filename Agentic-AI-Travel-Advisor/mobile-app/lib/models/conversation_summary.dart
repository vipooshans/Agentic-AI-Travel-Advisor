class ConversationSummary {
  final int id;
  final String title;
  final DateTime updatedAt;

  const ConversationSummary({required this.id, required this.title, required this.updatedAt});

  factory ConversationSummary.fromJson(Map<String, dynamic> json) {
    return ConversationSummary(
      id: (json['id'] as num).toInt(),
      title: json['title'] as String? ?? 'Conversation',
      updatedAt: DateTime.parse(json['updatedAt'] as String),
    );
  }
}
